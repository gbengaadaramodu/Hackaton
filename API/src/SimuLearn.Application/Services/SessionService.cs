using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Common;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Simulation;
using SimuLearn.Domain.Analytics;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Requests;
using SimuLearn.Domain.Sessions;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Tutoring;

namespace SimuLearn.Application.Services;

/// <summary>
/// The learning loop: start a session from an approved configuration, apply validated parameter
/// changes, compare states and ask the grounded tutor. The server remains the single authority for
/// state and calculations (FR-030 - FR-038).
/// </summary>
public sealed class SessionService(
    IApplicationDbContext db,
    SimulationModelRegistry registry,
    ITutorNarrator tutor,
    ILearningEventRecorder events)
{
    private const int MaxTutorQuestionLength = 1000;

    private readonly SimulationCommandParser _commandParser = new();

    public async Task<SessionDto> StartAsync(
        Actor actor,
        StartSessionRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await ResolveAssignmentAsync(actor, request.AssignmentId, cancellationToken);
        var simulationRequest = await ResolveSimulationRequestAsync(actor, request.SimulationRequestId, cancellationToken);
        var lessonId = request.LessonId ?? assignment?.LessonId;
        var lesson = await ResolveLessonAsync(actor, lessonId, cancellationToken);

        var scene = simulationRequest is null
            ? null
            : JsonDefaults.Deserialize<SceneSpecification>(simulationRequest.SceneJson);

        if (simulationRequest is not null && simulationRequest.Status != SimulationRequestStatus.Completed)
        {
            throw new ConflictException(
                "That simulation request has not completed successfully. Retry it or choose a template.",
                "request_not_ready");
        }

        var modelKey = request.ModelKey ?? scene?.SimulationModel ?? lesson?.ModelKey;
        if (string.IsNullOrWhiteSpace(modelKey))
        {
            throw new DomainValidationException(
                "A session needs a simulation.",
                new Dictionary<string, string[]>
                {
                    ["modelKey"] = ["Provide modelKey, lessonId, assignmentId or simulationRequestId."],
                });
        }

        if (scene is not null
            && !string.IsNullOrWhiteSpace(request.ModelKey)
            && !string.Equals(scene.SimulationModel, request.ModelKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException(
                "The simulation request belongs to a different model.",
                new Dictionary<string, string[]>
                {
                    ["modelKey"] = [$"That request produced '{scene.SimulationModel}'."],
                });
        }

        var model = registry.GetRequired(modelKey);

        var configuredState = request.InitialState
                              ?? scene?.InitialState
                              ?? LessonState(lesson)
                              ?? new Dictionary<string, double>();

        var parsed = new ParameterSet(configuredState);
        var validation = model.Validate(parsed);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                "The starting configuration is not valid for this simulation.",
                validation.ToErrors());
        }

        var state = model.Normalize(parsed);
        var templateId = await db.SimulationTemplates
            .Where(t => t.ModelKey == model.ModelKey)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken) ?? Guid.Empty;

        var session = new SimulationSession
        {
            UserId = actor.Id,
            ModelKey = model.ModelKey,
            TemplateId = templateId,
            LessonId = lesson?.Id,
            AssignmentId = assignment?.Id,
            SimulationRequestId = simulationRequest?.Id,
            Status = SessionStatus.Active,
            InitialStateJson = SessionStateSerializer.Serialize(state),
            StateJson = SessionStateSerializer.Serialize(state),
        };

        db.Sessions.Add(session);
        AddSnapshot(session, SnapshotReason.Created, actor.Id);
        await db.SaveChangesAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.SessionStarted, session.Id, payload: new
        {
            modelKey = model.ModelKey,
            lessonId = session.LessonId,
            assignmentId = session.AssignmentId,
        });

        await db.SaveChangesAsync(cancellationToken);
        return Build(session);
    }

    public async Task<SessionDto> GetAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var session = await LoadAsync(id, cancellationToken);
        if (!await SessionAccess.CanViewAsync(db, actor, session, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this session.");
        }

        return Build(session);
    }

    public async Task<PagedResult<SessionSummaryDto>> ListAsync(
        Actor actor,
        Guid? lessonId,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        IQueryable<SimulationSession> query = db.Sessions.AsNoTracking();

        if (actor.IsAdministratorOrReviewer)
        {
            // Reviewers and administrators can audit the full set.
        }
        else if (actor.IsTeacher)
        {
            var authoredLessonIds = db.Lessons.Where(l => l.AuthorId == actor.Id).Select(l => l.Id);
            query = query.Where(s =>
                s.UserId == actor.Id
                || (s.LessonId != null && authoredLessonIds.Contains(s.LessonId.Value)));
        }
        else
        {
            query = query.Where(s => s.UserId == actor.Id);
        }

        if (lessonId is { } filter)
        {
            query = query.Where(s => s.LessonId == filter);
        }

        var total = await query.CountAsync(cancellationToken);
        var sessions = await query
            .OrderByDescending(s => s.LastActivityAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = sessions.Select(session =>
        {
            var model = registry.Find(session.ModelKey);
            return new SessionSummaryDto(
                session.Id,
                session.ModelKey,
                model?.Title ?? session.ModelKey,
                session.Status,
                session.LessonId,
                session.AssignmentId,
                session.Version,
                session.CommandCount,
                session.MessageCount,
                session.StartedAt,
                session.LastActivityAt,
                session.EndedAt);
        }).ToList();

        return new PagedResult<SessionSummaryDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    public async Task<CommandResultDto> SubmitCommandAsync(
        Actor actor,
        Guid id,
        SubmitCommandRequest request,
        CancellationToken cancellationToken)
    {
        var session = await LoadForWriteAsync(actor, id, cancellationToken);

        if (session.Status is SessionStatus.Completed or SessionStatus.Abandoned)
        {
            throw new ConflictException("This session is closed; start a new session to keep exploring.", "session_closed");
        }

        if (request.ExpectedVersion is { } expected && expected != session.Version)
        {
            throw new ConflictException(
                $"The session has moved on to version {session.Version}.",
                "version_conflict");
        }

        var model = registry.GetRequired(session.ModelKey);
        var beforeState = SessionStateSerializer.Deserialize(session.StateJson);
        var before = new ParameterSet(beforeState);

        var (parameterName, value, parserExplanation) = ResolveCommand(request, model, before);

        var parameter = model.Parameters.FirstOrDefault(p =>
            string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            ?? throw Validation("command.parameter", $"'{parameterName}' is not a parameter of this simulation.");

        if (double.IsNaN(value) || double.IsInfinity(value) || !parameter.Contains(value))
        {
            throw Validation(
                "command.value",
                $"{parameter.Label} must be between {ParameterSet.Format(parameter.Min)} and " +
                $"{ParameterSet.Format(parameter.Max)} {parameter.Unit}.");
        }

        if (parameter.HasIntegralValuesOnly && Math.Abs(value % 1d) > 1e-9)
        {
            throw Validation("command.value", $"{parameter.Label} must be a whole number of {parameter.Unit}.");
        }

        var afterState = before.With(parameter.Name, value);
        var after = model.Normalize(afterState);

        var afterValidation = model.Validate(after);
        if (!afterValidation.IsValid)
        {
            throw new DomainValidationException(
                "The requested change is not valid for this simulation.",
                afterValidation.ToErrors());
        }

        var beforeOutcome = model.Simulate(before);
        var afterOutcome = model.Simulate(after);

        session.StateJson = SessionStateSerializer.Serialize(after);
        session.Version++;
        session.CommandCount++;
        session.LastActivityAt = DateTimeOffset.UtcNow;
        AddSnapshot(session, SnapshotReason.Command, actor.Id);

        var explanation = string.Join(
            " ",
            new[]
            {
                request.Reason is { Length: > 0 } reason ? $"Goal: {Sanitize(reason, 200)}." : null,
                parserExplanation,
                model.ExplainChange(before, beforeOutcome, after, afterOutcome),
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

        await SaveWithConcurrencyAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.ParameterChanged, session.Id, payload: new
        {
            parameter = parameter.Name,
            previous = before.TryGet(parameter.Name, out var previous) ? previous : (double?)null,
            value,
            version = session.Version,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new CommandResultDto(
            session.Id,
            session.Version,
            session.Status,
            after.ToDictionary(),
            SimulationMapper.ToResult(model, after, afterOutcome),
            explanation);
    }

    public async Task<SessionDto> ResetAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var session = await LoadForWriteAsync(actor, id, cancellationToken);
        if (session.Status is SessionStatus.Completed or SessionStatus.Abandoned)
        {
            throw new ConflictException("This session is closed; start a new session instead.", "session_closed");
        }

        session.StateJson = session.InitialStateJson;
        session.Version++;
        session.LastActivityAt = DateTimeOffset.UtcNow;
        AddSnapshot(session, SnapshotReason.Reset, actor.Id);

        await SaveWithConcurrencyAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.SessionReset, session.Id, payload: new { version = session.Version });
        await db.SaveChangesAsync(cancellationToken);

        return Build(session);
    }

    public async Task<SessionDto> ChangeStatusAsync(
        Actor actor,
        Guid id,
        SessionStatus status,
        CancellationToken cancellationToken)
    {
        var session = await LoadAsync(id, cancellationToken);
        if (!await SessionAccess.CanManageAsync(db, actor, session, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this session.");
        }

        if (!Enum.IsDefined(status))
        {
            throw Validation("status", "Unknown session status.");
        }

        if ((session.Status is SessionStatus.Completed or SessionStatus.Abandoned)
            && status != session.Status)
        {
            throw new ConflictException("A closed session cannot be reopened.", "session_closed");
        }

        session.Status = status;
        session.LastActivityAt = DateTimeOffset.UtcNow;

        LearningEventType eventType;
        switch (status)
        {
            case SessionStatus.Paused:
                eventType = LearningEventType.SessionPaused;
                break;
            case SessionStatus.Completed:
                eventType = LearningEventType.SessionCompleted;
                session.EndedAt = DateTimeOffset.UtcNow;
                AddSnapshot(session, SnapshotReason.Completed, actor.Id);
                break;
            case SessionStatus.Abandoned:
                eventType = LearningEventType.SessionCompleted;
                session.EndedAt = DateTimeOffset.UtcNow;
                break;
            default:
                eventType = LearningEventType.SessionResumed;
                session.EndedAt = null;
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        events.Record(actor.Id, eventType, session.Id, payload: new { status = status.ToString() });
        await db.SaveChangesAsync(cancellationToken);

        return Build(session);
    }

    public async Task<ComparisonResultDto> CompareAsync(
        Actor actor,
        Guid id,
        ComparisonRequest request,
        CancellationToken cancellationToken)
    {
        var session = await LoadAsync(id, cancellationToken);
        if (!await SessionAccess.CanViewAsync(db, actor, session, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this session.");
        }

        var model = registry.GetRequired(session.ModelKey);

        var baselineState = new ParameterSet(SessionStateSerializer.Deserialize(session.InitialStateJson));
        var candidateParsed = new ParameterSet(request.State ?? new Dictionary<string, double>());
        var validation = model.Validate(candidateParsed);
        if (!validation.IsValid)
        {
            throw new DomainValidationException("The comparison state is not valid.", validation.ToErrors());
        }

        var candidateState = model.Normalize(candidateParsed);
        var baselineOutcome = model.Simulate(baselineState);
        var candidateOutcome = model.Simulate(candidateState);

        var deltas = model.Parameters
            .Where(p => baselineState.TryGet(p.Name, out _) && candidateState.TryGet(p.Name, out _))
            .Select(p => new ParameterDelta(
                p.Name,
                p.Label,
                baselineState[p.Name],
                candidateState[p.Name],
                p.Unit))
            .ToList();

        var explanation = model.ExplainChange(baselineState, baselineOutcome, candidateState, candidateOutcome);

        await db.SaveChangesAsync(cancellationToken);
        events.Record(actor.Id, LearningEventType.ComparisonRun, session.Id, payload: new { deltas = deltas.Count });
        await db.SaveChangesAsync(cancellationToken);

        return new ComparisonResultDto(
            SimulationMapper.ToResult(model, baselineState, baselineOutcome),
            SimulationMapper.ToResult(model, candidateState, candidateOutcome),
            deltas,
            explanation);
    }

    public async Task<TutorMessageResponse> AskTutorAsync(
        Actor actor,
        Guid id,
        TutorMessageRequest request,
        CancellationToken cancellationToken)
    {
        var session = await LoadAsync(id, cancellationToken);
        if (session.UserId != actor.Id)
        {
            throw new ForbiddenException("Only the learner who started this session can ask it questions.");
        }

        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length is 0 or > MaxTutorQuestionLength)
        {
            throw Validation("message", $"Ask a question between 1 and {MaxTutorQuestionLength} characters.");
        }

        if (!Enum.IsDefined(request.Level))
        {
            throw Validation("level", "Unknown explanation level.");
        }

        var model = registry.GetRequired(session.ModelKey);
        var state = new ParameterSet(SessionStateSerializer.Deserialize(session.StateJson));
        var outcome = model.Simulate(state);
        var baselineState = new ParameterSet(SessionStateSerializer.Deserialize(session.InitialStateJson));
        var baselineOutcome = model.Simulate(baselineState);

        var conversation = await db.Conversations
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.SessionId == session.Id, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation { SessionId = session.Id, UserId = session.UserId };
            db.Conversations.Add(conversation);
        }

        var history = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(10)
            .Select(m => new TutorHistoryEntry(m.Role == MessageRole.Student ? "student" : "tutor", m.Content))
            .ToList();

        var reply = await tutor.AnswerAsync(
            message,
            request.Level,
            new TutorContext
            {
                Model = model,
                State = state,
                Outcome = outcome,
                BaselineState = baselineState,
                BaselineOutcome = baselineOutcome,
                History = history,
            },
            cancellationToken);

        var studentMessage = new Message
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Student,
            Content = message,
            Level = request.Level,
            IsAiGenerated = false,
        };

        var tutorMessage = new Message
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Tutor,
            Content = reply.Text,
            Level = request.Level,
            IsAiGenerated = true,
            Provider = reply.Provider,
            GroundingJson = JsonDefaults.Serialize(reply.GroundedFacts.Select(f => new
            {
                f.Name,
                f.Value,
                f.Unit,
            })),
        };

        db.Messages.Add(studentMessage);
        db.Messages.Add(tutorMessage);

        session.MessageCount += 2;
        session.LastActivityAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.TutorQuestionAsked, session.Id, payload: new
        {
            level = request.Level.ToString(),
            provider = reply.Provider,
            usedFallback = reply.UsedFallback,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new TutorMessageResponse(
            tutorMessage.Id,
            reply.Text,
            IsAiGenerated: true,
            reply.Provider,
            reply.UsedFallback,
            reply.GroundedFacts
                .Select(f => new GroundedFactDto(f.Name, f.Label, f.Value, f.Unit))
                .ToList());
    }

    public async Task<IReadOnlyList<TutorMessageDto>> GetMessagesAsync(
        Actor actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await LoadAsync(id, cancellationToken);
        if (!await SessionAccess.CanViewAsync(db, actor, session, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this session.");
        }

        var conversation = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.SessionId == session.Id, cancellationToken);

        if (conversation is null)
        {
            return [];
        }

        return conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TutorMessageDto(
                m.Id,
                m.Role == MessageRole.Student ? "student" : "tutor",
                m.Content,
                m.IsAiGenerated,
                m.Provider,
                m.CreatedAt))
            .ToList();
    }

    private async Task<SimulationSession> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Sessions
            .Include(s => s.Snapshots)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Session", id);

    private async Task<SimulationSession> LoadForWriteAsync(
        Actor actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await LoadAsync(id, cancellationToken);
        if (session.UserId != actor.Id)
        {
            throw new ForbiddenException("Only the learner who started this session can change it.");
        }

        return session;
    }

    private (string Parameter, double Value, string? Explanation) ResolveCommand(
        SubmitCommandRequest request,
        ISimulationModel model,
        ParameterSet state)
    {
        if (request.Command is { } command)
        {
            var type = (command.Type ?? string.Empty).Trim().Replace("_", "-", StringComparison.Ordinal).ToLowerInvariant();
            if (type is not ("set-parameter" or "setparameter" or "parameter" or "set"))
            {
                throw Validation(
                    "command.type",
                    $"Unsupported command '{command.Type}'. Supported commands: set-parameter.");
            }

            if (string.IsNullOrWhiteSpace(command.Parameter))
            {
                throw Validation("command.parameter", "Name the parameter to change.");
            }

            if (command.Value is not { } value)
            {
                throw Validation("command.value", "Provide a numeric value.");
            }

            var parameter = model.Parameters.FirstOrDefault(p =>
                string.Equals(p.Name, command.Parameter.Trim(), StringComparison.OrdinalIgnoreCase));

            var explanation = parameter is null
                ? null
                : $"Setting {parameter.Label.ToLowerInvariant()} to {ParameterSet.Format(value)} {parameter.Unit}.";
            return (command.Parameter.Trim(), value, explanation);
        }

        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            var parsed = _commandParser.Parse(request.Text, model, state)
                ?? throw new UnsupportedRequestException(
                    "That instruction could not be mapped to a supported parameter change. Try naming a parameter " +
                    "and a value, for example \"set the angle to 30 degrees\".",
                    [
                        $"Try: \"double the {model.Parameters[0].Label.ToLowerInvariant()}\" or " +
                        $"\"set {model.Parameters[0].Label.ToLowerInvariant()} to {ParameterSet.Format(model.Parameters[0].Default)}\".",
                    ]);

            return (parsed.Parameter, parsed.Value, parsed.Explanation);
        }

        throw Validation("command", "Provide a structured command or a natural-language instruction.");
    }

    private SessionDto Build(SimulationSession session)
    {
        var model = registry.GetRequired(session.ModelKey);
        var state = model.Normalize(new ParameterSet(SessionStateSerializer.Deserialize(session.StateJson)));
        var outcome = model.Simulate(state);

        return new SessionDto(
            session.Id,
            session.ModelKey,
            SimulationMapper.ToSummary(model),
            session.Status,
            session.Version,
            state.ToDictionary(),
            SessionStateSerializer.Deserialize(session.InitialStateJson),
            SimulationMapper.ToResult(model, state, outcome),
            session.LessonId,
            session.AssignmentId,
            session.StartedAt,
            session.LastActivityAt,
            session.EndedAt);
    }

    private async Task<SimulationRequest?> ResolveSimulationRequestAsync(
        Actor actor,
        Guid? requestId,
        CancellationToken cancellationToken)
    {
        if (requestId is not { } id)
        {
            return null;
        }

        var request = await db.SimulationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Simulation request", id);

        if (request.UserId != actor.Id)
        {
            throw new ForbiddenException("That simulation request belongs to another account.");
        }

        return request;
    }

    private async Task<Lesson?> ResolveLessonAsync(
        Actor actor,
        Guid? lessonId,
        CancellationToken cancellationToken)
    {
        if (lessonId is not { } id)
        {
            return null;
        }

        var lesson = await db.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        var canUse = lesson.Status == LessonStatus.Published
                     || lesson.AuthorId == actor.Id
                     || actor.IsAdministratorOrReviewer;

        if (!canUse)
        {
            throw new ForbiddenException("That lesson has not been published yet.");
        }

        return lesson;
    }

    private async Task<SimuLearn.Domain.Assignments.Assignment?> ResolveAssignmentAsync(
        Actor actor,
        Guid? assignmentId,
        CancellationToken cancellationToken)
    {
        if (assignmentId is not { } id)
        {
            return null;
        }

        var assignment = await db.Assignments
            .AsNoTracking()
            .Include(a => a.ClassRoom)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assignment", id);

        var isStudent = await db.ClassMembers
            .AnyAsync(m => m.ClassRoomId == assignment.ClassRoomId && m.StudentId == actor.Id, cancellationToken);

        var isTeacher = assignment.ClassRoom?.TeacherId == actor.Id;

        if (!isStudent && !isTeacher && !actor.IsAdministrator)
        {
            throw new ForbiddenException("This assignment is not available to your account.");
        }

        return assignment;
    }

    private static Dictionary<string, double>? LessonState(Lesson? lesson)
    {
        if (lesson is null)
        {
            return null;
        }

        var json = lesson.PublishedConfigurationJson ?? lesson.DraftConfigurationJson;
        var configuration = JsonDefaults.Deserialize<LessonConfiguration>(json);
        return configuration?.InitialState is { Count: > 0 } state ? state : null;
    }

    /// <summary>
    /// Adds an immutable state snapshot explicitly to the context. Adding it through the tracked
    /// navigation would make EF treat the populated key as an existing row and issue an UPDATE.
    /// </summary>
    private void AddSnapshot(SimulationSession session, SnapshotReason reason, Guid actorId)
    {
        db.SessionSnapshots.Add(new SessionSnapshot
        {
            SessionId = session.Id,
            StateJson = session.StateJson,
            Reason = reason,
            Version = session.Version,
            ActorId = actorId,
        });
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Another change updated this session at the same time. Reload and try again.",
                "version_conflict");
        }
    }

    private static DomainValidationException Validation(string field, string message) =>
        new("The command is not valid.",
            new Dictionary<string, string[]> { [field] = [message] });

    private static string Sanitize(string value, int maxLength)
    {
        var trimmed = value.Trim().Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
