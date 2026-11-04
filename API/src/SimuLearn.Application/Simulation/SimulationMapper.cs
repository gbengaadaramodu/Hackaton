using SimuLearn.Application.Contracts;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Simulation;

/// <summary>Maps authoritative model output onto API contracts. Calculations are never repeated here.</summary>
public static class SimulationMapper
{
    public static SimulationModelSummary ToSummary(ISimulationModel model) =>
        new(
            model.ModelKey,
            model.Title,
            model.Subject,
            model.Domain,
            model.SchemaVersion,
            model.LearningObjective,
            model.LearningObjectiveCode,
            model.GoverningRelationship,
            model.Renderer);

    public static SimulationResultDto ToResult(ISimulationModel model, ParameterSet state, SimulationOutcome outcome) =>
        new(
            ToSummary(model),
            state.ToDictionary(),
            outcome.Observations,
            outcome.Trajectory,
            outcome.Summary,
            outcome.IsInMotion,
            outcome.Warnings);

    public static SimulationTemplateDto ToTemplate(ISimulationModel model) =>
        new(
            model.ModelKey,
            model.Title,
            $"{model.LearningObjective} ({model.GoverningRelationship})",
            model.Subject,
            model.Domain,
            model.SchemaVersion,
            model.LearningObjective,
            model.LearningObjectiveCode,
            model.GoverningRelationship,
            model.Renderer,
            model.Parameters,
            model.Concepts,
            model.Features,
            model.Assumptions,
            model.Limitations,
            SceneSpecificationFactory.Create(model));

    public static SceneSpecification ToScene(ISimulationModel model, ParameterSet state) =>
        SceneSpecificationFactory.Create(model, state);
}
