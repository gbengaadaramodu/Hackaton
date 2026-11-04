using System.Globalization;

namespace SimuLearn.Domain.Assessments;

public enum QuestionType
{
    SingleChoice = 0,
    MultipleChoice = 1,
    Numeric = 2,
}

public sealed record AssessmentOption(string Id, string Text);

public sealed class AssessmentQuestion
{
    public required string Id { get; set; }

    public required string Prompt { get; set; }

    public QuestionType Type { get; set; } = QuestionType.SingleChoice;

    public List<AssessmentOption> Options { get; set; } = [];

    public List<string> CorrectOptionIds { get; set; } = [];

    public double? NumericAnswer { get; set; }

    /// <summary>Absolute tolerance for numeric answers. Defaults to 1% of the correct answer.</summary>
    public double? NumericTolerance { get; set; }

    public required string Explanation { get; set; }

    public string? LearningObjectiveCode { get; set; }
}

public sealed record AssessmentAnswer(
    string QuestionId,
    List<string>? SelectedOptionIds = null,
    double? NumericValue = null);

public sealed record QuestionFeedback(
    string QuestionId,
    bool Correct,
    string CorrectAnswer,
    string? SubmittedAnswer,
    string Explanation,
    double Points);

public sealed record AssessmentResult(
    double Score,
    double MaxScore,
    double Percentage,
    bool Passed,
    IReadOnlyList<QuestionFeedback> Feedback);

/// <summary>
/// Deterministic scoring for objective questions (FR-061). No AI is involved in deciding whether
/// an answer is correct, and multi-select questions require an exact match.
/// </summary>
public static class AssessmentScorer
{
    public static AssessmentResult Score(
        IReadOnlyList<AssessmentQuestion> questions,
        IReadOnlyList<AssessmentAnswer> answers,
        double passPercentage = 60d)
    {
        var answerLookup = new Dictionary<string, AssessmentAnswer>(StringComparer.OrdinalIgnoreCase);
        foreach (var answer in answers)
        {
            if (!string.IsNullOrWhiteSpace(answer.QuestionId))
            {
                answerLookup[answer.QuestionId.Trim()] = answer;
            }
        }

        var feedback = new List<QuestionFeedback>(questions.Count);
        foreach (var question in questions)
        {
            answerLookup.TryGetValue(question.Id, out var answer);
            var correct = IsCorrect(question, answer);
            feedback.Add(new QuestionFeedback(
                question.Id,
                correct,
                DescribeCorrectAnswer(question),
                DescribeSubmittedAnswer(question, answer),
                question.Explanation,
                correct ? 1d : 0d));
        }

        var score = feedback.Sum(f => f.Points);
        var maxScore = questions.Count;
        var percentage = maxScore == 0 ? 0d : Math.Round(score / maxScore * 100d, 1, MidpointRounding.AwayFromZero);

        return new AssessmentResult(score, maxScore, percentage, maxScore > 0 && percentage >= passPercentage, feedback);
    }

    private static bool IsCorrect(AssessmentQuestion question, AssessmentAnswer? answer)
    {
        if (answer is null)
        {
            return false;
        }

        return question.Type switch
        {
            QuestionType.SingleChoice or QuestionType.MultipleChoice => MatchesOptions(question, answer),
            QuestionType.Numeric => MatchesNumeric(question, answer),
            _ => false,
        };
    }

    private static bool MatchesOptions(AssessmentQuestion question, AssessmentAnswer answer)
    {
        var submitted = (answer.SelectedOptionIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var expected = question.CorrectOptionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return submitted.Length == expected.Length
               && submitted.Select(id => id.ToLowerInvariant()).SequenceEqual(expected.Select(id => id.ToLowerInvariant()));
    }

    private static bool MatchesNumeric(AssessmentQuestion question, AssessmentAnswer answer)
    {
        if (question.NumericAnswer is not { } expected || answer.NumericValue is not { } submitted)
        {
            return false;
        }

        if (double.IsNaN(submitted) || double.IsInfinity(submitted))
        {
            return false;
        }

        var tolerance = question.NumericTolerance ?? Math.Max(0.01d, Math.Abs(expected) * 0.01d);
        return Math.Abs(submitted - expected) <= tolerance + 1e-9;
    }

    private static string DescribeCorrectAnswer(AssessmentQuestion question)
    {
        if (question.Type == QuestionType.Numeric)
        {
            var tolerance = question.NumericTolerance ?? Math.Max(0.01d, Math.Abs(question.NumericAnswer ?? 0d) * 0.01d);
            return $"{Format(question.NumericAnswer ?? 0d)} (±{Format(tolerance)})";
        }

        var correct = question.CorrectOptionIds
            .Select(id => question.Options.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase))?.Text ?? id)
            .ToArray();

        return string.Join("; ", correct);
    }

    private static string? DescribeSubmittedAnswer(AssessmentQuestion question, AssessmentAnswer? answer)
    {
        if (answer is null)
        {
            return null;
        }

        if (question.Type == QuestionType.Numeric)
        {
            return answer.NumericValue is { } numeric ? Format(numeric) : null;
        }

        var selected = (answer.SelectedOptionIds ?? [])
            .Select(id => question.Options.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase))?.Text ?? id)
            .ToArray();

        return selected.Length == 0 ? null : string.Join("; ", selected);
    }

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
