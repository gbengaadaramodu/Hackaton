using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Ai;
using SimuLearn.Application.Common;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Simulation;
using SimuLearn.Domain.Analytics;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Requests;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Services;

/// <summary>
/// Owns the request → interpret → validate → scene pipeline. A request row is persisted before any
/// model call, so a provider failure never loses the learner's prompt (AC-005, NFR-010).
/// </summary>
public sealed class SimulationService(
    IApplicationDbContext db,
    SimulationModelRegistry registry,
    IRequestInterpreter interpreter,
    SceneSpecificationValidator sceneValidator,
    ILearningEventRecorder events)
{
    public const int MaxPromptLength = 2000;

    public Task<IReadOnlyList<SimulationTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<SimulationTemplateDto> templates = registry.All
            .OrderBy(m => m.Subject, StringComparer.Ordinal)
            .ThenBy(m => m.Title, StringComparer.Ordinal)
            .Select(SimulationMapper.ToTemplate)
            .ToList();

        return Task.FromResult(templates);
    }

    public Task<SimulationTemplateDto> GetTemplateAsync(string modelKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SimulationMapper.ToTemplate(registry.GetRequired(modelKey)));
    }

    public Task<SimulationResultDto> PreviewAsync(
        string modelKey,
        IReadOnlyDictionary<string, double>? state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var model = registry.GetRequired(modelKey);
        var parsed = new ParameterSet(state ?? new Dictionary<string, double>());
        var validation = model.Validate(parsed);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                "The supplied simulation state is not valid.",
                validation.ToErrors());
        }

        var normalized = model.Normalize(parsed);
        return Task.FromResult(SimulationMapper.ToResult(model, normalized, model.Simulate(normalized)));
    }

    public RenderSceneResponse RenderScene(
        string modelKey,
        IReadOnlyDictionary<string, double>? state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var model = registry.GetRequired(modelKey);
        var parsed = new ParameterSet(state ?? new Dictionary<string, double>());
        var validation = model.Validate(parsed);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                "The supplied simulation state is not valid.",
                validation.ToErrors());
        }

        var normalized = model.Normalize(parsed);
        var outcome = model.Simulate(normalized);
        var result = SimulationMapper.ToResult(model, normalized, outcome);
        var svg = SvgSceneRenderer.Render(model, normalized, outcome);
        return new RenderSceneResponse(model.ModelKey, model.Renderer, svg, result);
    }

    public async Task<SimulationRequestDto> CreateRequestAsync(
        Actor actor,
        CreateSimulationRequest request,
        CancellationToken cancellationToken)
    {
        var prompt = request.Prompt?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (prompt.Length == 0)
        {
            errors["prompt"] = ["Describe what you would like to explore."];
        }
        else if (prompt.Length > MaxPromptLength)
        {
            errors["prompt"] = [$"Keep the request under {MaxPromptLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("The simulation request is not valid.", errors);
        }

        var entity = new SimulationRequest
        {
            UserId = actor.Id,
            Prompt = prompt,
            Subject = Trim(request.Subject, 80),
            Level = Trim(request.Level, 40),
        };

        db.SimulationRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.SimulationRequested, payload: new
        {
            simulationRequestId = entity.Id,
            promptLength = prompt.Length,
            subject = entity.Subject,
            level = entity.Level,
        });

        try
        {
            entity.Provider = null;
            var interpretation = await interpreter.InterpretAsync(
                new InterpretationRequest(prompt, entity.Subject, entity.Level),
                cancellationToken);

            entity.Provider = interpretation.Provider;
            entity.UsedFallback = interpretation.UsedFallback;
            entity.InterpretationJson = JsonDefaults.Serialize(ToDto(interpretation));

            if (interpretation.Status != InterpretationStatus.Ready || interpretation.ModelKey is null)
            {
                entity.Status = interpretation.Status == InterpretationStatus.NeedsClarification
                    ? SimulationRequestStatus.NeedsClarification
                    : SimulationRequestStatus.Failed;
                entity.ClarifyingQuestionsJson = JsonDefaults.Serialize(interpretation.ClarifyingQuestions);
                entity.ErrorCode = interpretation.Status == InterpretationStatus.NeedsClarification
                    ? "clarification_required"
                    : "unsupported_request";
                entity.ErrorMessage = interpretation.Rationale;
                entity.CompletedAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(cancellationToken);
                return ToDto(entity);
            }

            var model = registry.GetRequired(interpretation.ModelKey);
            var state = model.Normalize(interpretation.State);
            var stateValidation = model.Validate(state);
            if (!stateValidation.IsValid)
            {
                throw new GenerationFailedException(
                    "The selected simulation state was outside the published parameter ranges.");
            }

            var specification = SceneSpecificationFactory.Create(
                model,
                state,
                source: interpretation.UsedFallback ? "template" : "generated",
                provider: interpretation.Provider);

            var sceneValidation = sceneValidator.Validate(specification, specification.Source, interpretation.Provider);
            if (!sceneValidation.IsValid || sceneValidation.Specification is null)
            {
                throw new GenerationFailedException(
                    "The generated scene configuration was rejected by the simulation schema.");
            }

            entity.SceneJson = JsonDefaults.Serialize(sceneValidation.Specification);
            entity.Status = SimulationRequestStatus.Completed;
            entity.ErrorCode = null;
            entity.ErrorMessage = null;
            entity.ClarifyingQuestionsJson = null;
            entity.CompletedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return ToDto(entity);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DomainException ex)
        {
            entity.Status = SimulationRequestStatus.Failed;
            entity.ErrorCode = ex.Code;
            entity.ErrorMessage = ex.Message;
            entity.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            entity.Status = SimulationRequestStatus.Failed;
            entity.ErrorCode = "generation_failed";
            entity.ErrorMessage = "The simulation could not be prepared. Retry or pick a template from the catalogue.";
            entity.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);

            throw new GenerationFailedException(entity.ErrorMessage, ex);
        }
    }

    public async Task<SimulationRequestDto> GetRequestAsync(
        Actor actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await db.SimulationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Simulation request", id);

        if (entity.UserId != actor.Id && !actor.IsAdministratorOrReviewer)
        {
            throw new ForbiddenException("You can only read your own simulation requests.");
        }

        return ToDto(entity);
    }

    public async Task<PagedResult<SimulationRequestDto>> ListRequestsAsync(
        Actor actor,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var query = db.SimulationRequests.AsNoTracking().Where(r => r.UserId == actor.Id);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SimulationRequestDto>(
            items.Select(ToDto).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            total);
    }

    private SimulationRequestDto ToDto(SimulationRequest entity)
    {
        var interpretation = JsonDefaults.Deserialize<InterpretationDto>(entity.InterpretationJson);
        var scene = JsonDefaults.Deserialize<SceneSpecification>(entity.SceneJson);
        var questions = JsonDefaults.Deserialize<List<string>>(entity.ClarifyingQuestionsJson) ?? [];

        return new SimulationRequestDto(
            entity.Id,
            entity.Prompt,
            entity.Subject,
            entity.Level,
            entity.Status,
            scene,
            interpretation,
            questions,
            entity.ErrorCode,
            entity.ErrorMessage,
            entity.UsedFallback,
            entity.Provider,
            entity.CreatedAt,
            entity.CompletedAt);
    }

    private static InterpretationDto ToDto(InterpretationResult result) =>
        new(
            result.Status,
            result.ModelKey,
            result.Subject,
            result.Topic,
            result.LearningObjective,
            result.Confidence,
            result.Rationale,
            result.State.ToDictionary(),
            result.Provider,
            result.UsedFallback);

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
