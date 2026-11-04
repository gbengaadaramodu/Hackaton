using System.Globalization;
using System.Text.RegularExpressions;

namespace SimuLearn.Application.Ai;

/// <summary>
/// Rejects generated explanations that contain a number the simulation did not produce. This is the
/// cheapest, most reliable defence against a fluent but fabricated physics answer (HAC-006, SRS 9.3).
/// </summary>
public static partial class GroundingGuard
{
    // Structural counts such as "2 states" or "1 model" are harmless; physical quantities are not.
    private static readonly HashSet<double> StructuralNumbers = [0d, 1d, 2d];

    [GeneratedRegex(@"\d+(?:[.,]\d+)?")]
    private static partial Regex NumberPattern();

    public static bool IsGrounded(
        string? text,
        IEnumerable<double> allowedValues,
        out string? offendingValue)
    {
        offendingValue = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var allowed = allowedValues
            .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .ToArray();

        foreach (Match match in NumberPattern().Matches(text))
        {
            var token = match.Value.Replace(",", ".", StringComparison.Ordinal);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            if (StructuralNumbers.Contains(number) && !token.Contains('.'))
            {
                continue;
            }

            var grounded = allowed.Any(value =>
                Math.Abs(value - number) <= Math.Max(0.01d, Math.Abs(value) * 0.005d));

            if (!grounded)
            {
                offendingValue = match.Value;
                return false;
            }
        }

        return true;
    }
}
