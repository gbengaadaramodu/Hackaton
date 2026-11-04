using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Sessions;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Tutoring;

namespace SimuLearn.Application.Contracts;

public sealed record SimulationModelSummary(
    string ModelKey,
    string Title,
    string Subject,
    string Domain,
    string SchemaVersion,
    string LearningObjective,
    string LearningObjectiveCode,
    string GoverningRelationship,
    string Renderer);

public sealed record SimulationResultDto(
    SimulationModelSummary Model,
    IReadOnlyDictionary<string, double> State,
    IReadOnlyList<SimulationObservation> Observations,
    IReadOnlyList<TrajectoryPoint> Trajectory,
    string Summary,
    bool IsInMotion,
    IReadOnlyList<string> Warnings);

public sealed record SimulationTemplateDto(
    string ModelKey,
    string Title,
    string Description,
    string Subject,
    string Domain,
    string SchemaVersion,
    string LearningObjective,
    string LearningObjectiveCode,
    string GoverningRelationship,
    string Renderer,
    IReadOnlyList<ParameterDefinition> Parameters,
    IReadOnlyList<ConceptNote> Concepts,
    IReadOnlyList<string> Features,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Limitations,
    SceneSpecification Scene);

public sealed record CreateSimulationRequest(string Prompt, string? Subject = null, string? Level = null);

public sealed record PreviewSimulationRequest(
    string ModelKey,
    Dictionary<string, double>? State = null);

public sealed record RenderSceneRequest(
    string ModelKey,
    Dictionary<string, double>? State = null);

public sealed record RenderSceneResponse(
    string ModelKey,
    string Renderer,
    string Svg,
    SimulationResultDto Result);

public sealed record InterpretationDto(
    InterpretationStatus Status,
    string? ModelKey,
    string? Subject,
    string? Topic,
    string? LearningObjective,
    double Confidence,
    string Rationale,
    IReadOnlyDictionary<string, double> State,
    string Provider,
    bool UsedFallback);

public sealed record SimulationRequestDto(
    Guid Id,
    string Prompt,
    string? Subject,
    string? Level,
    Domain.Requests.SimulationRequestStatus Status,
    SceneSpecification? Scene,
    InterpretationDto? Interpretation,
    IReadOnlyList<string> ClarifyingQuestions,
    string? ErrorCode,
    string? ErrorMessage,
    bool UsedFallback,
    string? Provider,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record StartSessionRequest(
    string? ModelKey = null,
    Guid? SimulationRequestId = null,
    Guid? LessonId = null,
    Guid? AssignmentId = null,
    Dictionary<string, double>? InitialState = null);

public sealed record SessionDto(
    Guid Id,
    string ModelKey,
    SimulationModelSummary Model,
    SessionStatus Status,
    int Version,
    IReadOnlyDictionary<string, double> State,
    IReadOnlyDictionary<string, double> InitialState,
    SimulationResultDto Result,
    Guid? LessonId,
    Guid? AssignmentId,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset? EndedAt);

public sealed record SimulationCommandDto(string Type, string? Parameter = null, double? Value = null);

public sealed record SubmitCommandRequest(
    SimulationCommandDto? Command = null,
    int? ExpectedVersion = null,
    string? Reason = null,
    string? Text = null);

public sealed record SessionSummaryDto(
    Guid Id,
    string ModelKey,
    string ModelTitle,
    SessionStatus Status,
    Guid? LessonId,
    Guid? AssignmentId,
    int Version,
    int CommandCount,
    int MessageCount,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset? EndedAt);

public sealed record CommandResultDto(
    Guid SessionId,
    int Version,
    SessionStatus Status,
    IReadOnlyDictionary<string, double> State,
    SimulationResultDto Result,
    string Explanation);

public sealed record SimulationStatusRequest(SessionStatus Status);

public sealed record ComparisonRequest(Dictionary<string, double> State);

public sealed record ParameterDelta(
    string Parameter,
    string Label,
    double Baseline,
    double Candidate,
    string Unit);

public sealed record ComparisonResultDto(
    SimulationResultDto Baseline,
    SimulationResultDto Candidate,
    IReadOnlyList<ParameterDelta> Deltas,
    string Explanation);

public sealed record TutorMessageRequest(string Message, ExplanationLevel Level = ExplanationLevel.Standard);

public sealed record GroundedFactDto(string Name, string Label, double Value, string Unit);

public sealed record TutorMessageResponse(
    Guid MessageId,
    string Reply,
    bool IsAiGenerated,
    string Provider,
    bool UsedFallback,
    IReadOnlyList<GroundedFactDto> GroundedFacts);

public sealed record TutorMessageDto(
    Guid Id,
    string Role,
    string Content,
    bool IsAiGenerated,
    string? Provider,
    DateTimeOffset CreatedAt);

public sealed record SubjectDto(Guid Id, string Code, string Name);

public sealed record ObjectiveDto(Guid Id, string Code, string Description, string Level, string SubjectCode);

public sealed record CatalogDto(
    IReadOnlyList<SubjectDto> Subjects,
    IReadOnlyList<ObjectiveDto> Objectives,
    IReadOnlyList<SimulationTemplateDto> Simulations);
