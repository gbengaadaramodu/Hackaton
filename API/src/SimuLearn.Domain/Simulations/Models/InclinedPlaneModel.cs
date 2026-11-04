namespace SimuLearn.Domain.Simulations.Models;

/// <summary>
/// MVP physics model: a rigid block sliding down a straight inclined plane.
/// Acceleration follows the validated classroom equation a = g(sin θ − μk cos θ); when the
/// friction limit equals or exceeds the driving component the block remains at rest.
/// </summary>
public sealed class InclinedPlaneModel : SimulationModelBase
{
    public const string Key = "inclined-plane-v1";
    public const double GravitationalAcceleration = 9.81;

    private const double FrameSeconds = 0.05;
    private const double MaxTraceSeconds = 15;

    public override string ModelKey => Key;

    public override string Domain => "Mechanics";

    public override string Title => "Block on an inclined plane";

    public override string LearningObjective =>
        "Understand how ramp angle, friction and mass affect the acceleration and motion of a block " +
        "sliding down an inclined plane.";

    public override string LearningObjectiveCode => "PHY-MECH-INCLINE";

    public override string GoverningRelationship => "a = g(sin θ − μk cos θ)";

    protected override IReadOnlyList<ParameterDefinition> ParameterDefinitions { get; } =
    [
        new("massKg", "Mass", 0.1, 20, 0.1, "kg", 2, "m"),
        new("angleDegrees", "Ramp angle", 0, 60, 1, "degrees", 20, "θ"),
        new("frictionCoefficient", "Kinetic friction coefficient", 0, 0.8, 0.01, "μ", 0.15, "μk"),
        new("rampLengthM", "Ramp length", 0.5, 10, 0.1, "m", 4, "L"),
    ];

    public override IReadOnlyList<string> Keywords { get; } =
    [
        "incline", "inclined plane", "ramp", "slope", "friction", "angle", "block", "gravity",
        "mass", "plane", "forces", "mechanics", "slides",
    ];

    public override IReadOnlyList<ConceptNote> Concepts { get; } =
    [
        new("Net force", "The sum of the forces acting along the plane: the weight component mg sin θ minus friction."),
        new("Normal force", "The support force perpendicular to the plane, equal to mg cos θ in this model."),
        new("Friction limit", "The largest friction the surface can provide, μk × normal force in this simplified model."),
        new("Acceleration", "How quickly velocity changes; here it stays constant while the block slides."),
    ];

    public override IReadOnlyList<string> Features { get; } =
        ["force-vectors", "motion-animation", "trajectory-trace", "measurement-panel"];

    public override IReadOnlyList<string> Assumptions { get; } =
    [
        "The block is a rigid body sliding on a fixed, straight plane.",
        "Kinetic friction follows a simple Coulomb model with a single coefficient.",
        "Air resistance, rolling and rotational effects are ignored.",
        "Gravitational acceleration is taken as 9.81 m/s² near the Earth's surface.",
    ];

    public override IReadOnlyList<string> Limitations { get; } =
    [
        "This is an idealised constant-acceleration model, not a quantitative prediction for rough or deformable surfaces.",
        "Static friction is treated as sufficient to hold the block whenever μk ≥ tan θ.",
        "Only straight-line sliding is simulated; the block cannot leave the plane or tip over.",
    ];

    protected override SimulationOutcome Compute(ParameterSet state)
    {
        var mass = state["massKg"];
        var angle = state["angleDegrees"];
        var mu = state["frictionCoefficient"];
        var length = state["rampLengthM"];

        var radians = angle * Math.PI / 180d;
        var sin = Math.Sin(radians);
        var cos = Math.Cos(radians);

        var weight = mass * GravitationalAcceleration;
        var weightAlongPlane = weight * sin;
        var normalForce = weight * cos;
        var frictionLimit = mu * normalForce;
        var netForce = weightAlongPlane - frictionLimit;
        var slides = angle > 0 && netForce > ComparisonTolerance;

        var acceleration = slides ? netForce / mass : 0d;
        var frictionForce = slides ? frictionLimit : weightAlongPlane;

        var travelTime = slides ? Math.Sqrt(2 * length / acceleration) : 0d;
        var finalVelocity = slides ? Math.Sqrt(2 * acceleration * length) : 0d;

        var observations = new List<SimulationObservation>
        {
            new("acceleration", "Acceleration along the plane", acceleration, "m/s²", "a"),
            new("netForce", "Net force along the plane", slides ? netForce : 0d, "N", "Fnet"),
            new("weightComponent", "Weight component along the plane", weightAlongPlane, "N", "mg sin θ"),
            new("normalForce", "Normal force", normalForce, "N", "N"),
            new("frictionForce", slides ? "Kinetic friction force" : "Static friction force", frictionForce, "N", "f"),
            new("frictionLimit", "Kinetic friction limit", frictionLimit, "N", "μk N"),
        };

        if (slides)
        {
            observations.Add(new SimulationObservation("finalVelocity", "Speed at the bottom", finalVelocity, "m/s", "v"));
            observations.Add(new SimulationObservation("travelTime", "Time to reach the bottom", travelTime, "s", "t"));
        }

        var warnings = new List<string>();
        if (!slides)
        {
            warnings.Add(
                $"The block stays at rest: the driving component ({Format(weightAlongPlane)} N) does not exceed " +
                $"the friction limit ({Format(frictionLimit)} N).");
        }

        var summary = slides
            ? $"With a {FormatTrim(angle)}° ramp, μk = {FormatTrim(mu)} and a {FormatTrim(length)} m run, " +
              $"the block accelerates at {Format(acceleration)} m/s² and reaches {Format(finalVelocity)} m/s " +
              $"at the bottom after {Format(travelTime)} s."
            : $"With a {FormatTrim(angle)}° ramp and μk = {FormatTrim(mu)}, the block does not slide: " +
              $"the friction limit ({Format(frictionLimit)} N) is at least the driving component " +
              $"({Format(weightAlongPlane)} N), so acceleration is 0 m/s².";

        var grounding = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = GravitationalAcceleration,
            ["massKg"] = mass,
            ["angleDegrees"] = angle,
            ["frictionCoefficient"] = mu,
            ["rampLengthM"] = length,
            ["weight"] = weight,
            ["weightComponent"] = weightAlongPlane,
            ["normalForce"] = normalForce,
            ["frictionLimit"] = frictionLimit,
            ["netForce"] = slides ? netForce : 0d,
            ["acceleration"] = acceleration,
            ["frictionForce"] = frictionForce,
            ["finalVelocity"] = finalVelocity,
            ["travelTime"] = travelTime,
        };

        return new SimulationOutcome
        {
            Observations = observations,
            Trajectory = BuildTrajectory(acceleration, length, travelTime, slides),
            Summary = summary,
            IsInMotion = slides,
            Warnings = warnings,
            GroundingValues = grounding,
        };
    }

    public override string ExplainChange(
        ParameterSet before,
        SimulationOutcome beforeOutcome,
        ParameterSet after,
        SimulationOutcome afterOutcome)
    {
        var parts = new List<string>();

        var angleChanged = Changed(before, after, "angleDegrees");
        var frictionChanged = Changed(before, after, "frictionCoefficient");
        var massChanged = Changed(before, after, "massKg");
        var lengthChanged = Changed(before, after, "rampLengthM");

        if (angleChanged)
        {
            parts.Add($"The ramp angle moved from {FormatTrim(before["angleDegrees"])}° to {FormatTrim(after["angleDegrees"])}°.");
        }

        if (frictionChanged)
        {
            parts.Add($"The kinetic friction coefficient moved from {FormatTrim(before["frictionCoefficient"])} to {FormatTrim(after["frictionCoefficient"])}.");
        }

        if (lengthChanged)
        {
            parts.Add($"The ramp length moved from {FormatTrim(before["rampLengthM"])} m to {FormatTrim(after["rampLengthM"])} m.");
        }

        if (massChanged)
        {
            parts.Add($"The mass moved from {FormatTrim(before["massKg"])} kg to {FormatTrim(after["massKg"])} kg.");
        }

        var wasMoving = beforeOutcome.IsInMotion;
        var isMoving = afterOutcome.IsInMotion;
        var previousAcceleration = beforeOutcome.Find("acceleration")?.Value ?? 0d;
        var currentAcceleration = afterOutcome.Find("acceleration")?.Value ?? 0d;

        if (!wasMoving && !isMoving)
        {
            parts.Add(
                $"The block still stays at rest because the friction limit is at least the component of weight " +
                $"along the plane (μk {FormatTrim(after["frictionCoefficient"])} ≥ tan {FormatTrim(after["angleDegrees"])}°).");
        }
        else if (!wasMoving && isMoving)
        {
            parts.Add(
                $"The block now slides: the driving component mg sin θ exceeds the friction limit, so it accelerates " +
                $"at {Format(currentAcceleration)} m/s² instead of staying at rest.");
        }
        else if (wasMoving && !isMoving)
        {
            parts.Add(
                $"The block now stays at rest because the friction limit has overtaken the driving component, " +
                $"so acceleration fell from {Format(previousAcceleration)} m/s² to 0 m/s².");
        }
        else
        {
            parts.Add($"Acceleration changed from {Format(previousAcceleration)} m/s² to {Format(currentAcceleration)} m/s².");

            if (angleChanged)
            {
                parts.Add(
                    "A steeper plane increases the component of weight along the plane (mg sin θ), which is what drives the block.");
            }

            if (frictionChanged)
            {
                parts.Add(
                    "More friction removes a larger share of that driving force (μk N), which is what slows the block.");
            }
        }

        if (massChanged && Math.Abs(currentAcceleration - previousAcceleration) <= 1e-6)
        {
            parts.Add(
                "Mass alone does not change the acceleration here: the driving component and the normal force both " +
                "scale with mass, so a = g(sin θ − μk cos θ) is unchanged.");
        }

        if (lengthChanged && isMoving)
        {
            parts.Add(
                $"With the {FormatTrim(after["rampLengthM"])} m run, the block reaches " +
                $"{Format(afterOutcome.Find("finalVelocity")?.Value ?? 0d)} m/s at the bottom.");
        }

        parts.Add("The model assumes a rigid block, a constant μk and g = 9.81 m/s², and ignores air resistance and rotation.");

        return string.Join(" ", parts);
    }

    private static List<TrajectoryPoint> BuildTrajectory(double acceleration, double length, double travelTime, bool slides)
    {
        if (!slides)
        {
            return [new TrajectoryPoint(0, 0, 0)];
        }

        var points = new List<TrajectoryPoint>();
        var end = Math.Min(travelTime, MaxTraceSeconds);

        for (var time = 0d; time < end; time += FrameSeconds)
        {
            var position = Math.Min(0.5 * acceleration * time * time, length);
            points.Add(new TrajectoryPoint(Round(time), Round(position), Round(acceleration * time)));
        }

        points.Add(new TrajectoryPoint(Round(end), Round(length), Round(acceleration * end)));
        return points;
    }

    private static bool Changed(ParameterSet before, ParameterSet after, string name) =>
        before.TryGet(name, out var previous)
        && after.TryGet(name, out var current)
        && Math.Abs(current - previous) > ComparisonTolerance;

    private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
