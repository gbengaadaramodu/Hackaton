namespace SimuLearn.Domain.Simulations.Models;

/// <summary>
/// History cause-and-effect model: how literacy, the press and organised nationalism
/// built momentum toward Nigerian independence between amalgamation (1914) and
/// independence (1960). The index is a teaching aid, not a historical measurement.
/// </summary>
public sealed class NigerianIndependenceModel : SimulationModelBase
{
    public const string Key = "nigerian-independence-v1";

    public override string ModelKey => Key;

    public override string Subject => "History";

    public override string Domain => "Nigerian nationalism";

    public override string Title => "The road to Nigerian independence";

    public override string LearningObjective =>
        "Understand how literacy, a free press and organised nationalism built momentum " +
        "for independence between 1914 and 1960.";

    public override string LearningObjectiveCode => "HIST-NA-INDEPENDENCE";

    public override string GoverningRelationship =>
        "Readiness = 0.45 Mobilisation + 0.30 Press + 0.25 Literacy - 0.15 Restrictiveness + time bonus";

    protected override IReadOnlyList<ParameterDefinition> ParameterDefinitions { get; } =
    [
        new("focusYear", "Focus year", 1914, 1960, 1, "year", 1950, "Year"),
        new("literacyPercent", "Literacy rate", 0, 80, 1, "%", 35, "Lit"),
        new("pressFreedomIndex", "Press freedom", 0, 100, 1, "index", 55, "Press"),
        new("nationalistMobilisation", "Nationalist mobilisation", 0, 100, 1, "index", 60, "Mob"),
        new("colonialRestrictiveness", "Colonial restrictiveness", 0, 100, 1, "index", 40, "Rest"),
    ];

    public override IReadOnlyList<string> Keywords { get; } =
    [
        "history", "nigeria", "nigerian", "independence", "nationalist", "nationalism",
        "colonial", "amalgamation", "press", "literacy", "movement", "freedom", "self-government",
    ];

    public override IReadOnlyList<ConceptNote> Concepts { get; } =
    [
        new("Amalgamation", "The 1914 joining of the Northern and Southern protectorates into one colony called Nigeria."),
        new("Literacy", "How many people could read newspapers, pamphlets and party messages, which spread political ideas."),
        new("Free press", "Newspapers such as the West African Pilot that carried nationalist arguments to readers."),
        new("Mobilisation", "Parties, unions and town meetings that turned private opinion into organised pressure."),
        new("Constitutional talks", "Negotiated steps toward self-government that led to independence on 1 October 1960."),
    ];

    public override IReadOnlyList<string> Features { get; } =
        ["timeline-scrubber", "readiness-dial", "cause-effect-bars", "measurement-panel"];

    public override IReadOnlyList<string> Assumptions { get; } =
    [
        "The readiness index is a classroom simplification that weights mobilisation, press, literacy and restrictiveness.",
        "The focus year sets the time bonus: pressure built faster after the Second World War.",
        "Regional differences between North, West and East are combined into one classroom index.",
    ];

    public override IReadOnlyList<string> Limitations { get; } =
    [
        "The index is not a measured historical quantity; it only makes cause and effect visible for discussion.",
        "Real independence turned on regional politics, constitutional conferences and events this model does not list.",
        "Only the 1914-1960 window is covered; later history is out of scope.",
    ];

    protected override SimulationOutcome Compute(ParameterSet state)
    {
        var year = state["focusYear"];
        var literacy = state["literacyPercent"];
        var press = state["pressFreedomIndex"];
        var mobilisation = state["nationalistMobilisation"];
        var restrictiveness = state["colonialRestrictiveness"];

        var literacyScaled = literacy / 80d * 100d;
        var timeBonus = year <= 1945 ? 0d : Math.Min(10d, (year - 1945) / 15d * 10d);
        var readiness = Math.Clamp(
            0.45 * mobilisation + 0.30 * press + 0.25 * literacyScaled - 0.15 * restrictiveness + timeBonus,
            0d, 100d);
        var momentum = mobilisation * press / 100d;
        var timelineYears = year - 1914d;

        var observations = new List<SimulationObservation>
        {
            new("readinessIndex", "Independence readiness index", Math.Round(readiness, 2), "index", "R"),
            new("movementMomentum", "Movement momentum", Math.Round(momentum, 2), "index", "M"),
            new("timelineYears", "Years since amalgamation", timelineYears, "years", "Y"),
            new("literacyScaled", "Literacy scaled to 100", Math.Round(literacyScaled, 2), "index", "L"),
        };

        var warnings = new List<string>();
        if (readiness < 45d)
        {
            warnings.Add("Readiness is still low: raise literacy, press freedom or mobilisation to see momentum grow.");
        }

        if (year >= 1960 && readiness < 70d)
        {
            warnings.Add("By 1960 independence arrived even when this simple index is below 70; the index omits conference politics.");
        }

        var outlook = readiness >= 70d
            ? "Strong momentum toward self-government."
            : readiness >= 45d
                ? "Momentum is building but independence is not yet secure."
                : "Momentum is limited and self-government still looks distant.";

        var summary =
            $"By {FormatTrim(year)}, {FormatTrim(timelineYears)} years after amalgamation, readiness is " +
            $"{Format(readiness)} with movement momentum {Format(momentum)}. {outlook}";

        var grounding = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["focusYear"] = year,
            ["literacyPercent"] = literacy,
            ["literacyScaled"] = literacyScaled,
            ["pressFreedomIndex"] = press,
            ["nationalistMobilisation"] = mobilisation,
            ["colonialRestrictiveness"] = restrictiveness,
            ["readinessIndex"] = readiness,
            ["movementMomentum"] = momentum,
            ["timelineYears"] = timelineYears,
            ["timeBonus"] = timeBonus,
        };

        return new SimulationOutcome
        {
            Observations = observations,
            Trajectory = BuildTrajectory(year, readiness, timelineYears),
            Summary = summary,
            IsInMotion = readiness >= 45d,
            Warnings = warnings,
            GroundingValues = grounding,
        };
    }

    private static List<TrajectoryPoint> BuildTrajectory(double focusYear, double readiness, double timelineYears)
    {
        if (timelineYears <= 0)
        {
            return [new TrajectoryPoint(Round(focusYear), 0, 0)];
        }

        var points = new List<TrajectoryPoint>();
        var steps = 10;
        var velocity = readiness / timelineYears;
        for (var i = 0; i <= steps; i++)
        {
            var progress = i / (double)steps;
            var year = 1914 + timelineYears * progress;
            points.Add(new TrajectoryPoint(Round(year), Round(readiness * progress), Round(velocity)));
        }

        return points;
    }

    private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    public override string ExplainChange(
        ParameterSet before,
        SimulationOutcome beforeOutcome,
        ParameterSet after,
        SimulationOutcome afterOutcome)
    {
        var parts = new List<string>();

        if (Changed(before, after, "focusYear"))
        {
            parts.Add($"The focus year moved from {FormatTrim(before["focusYear"])} to {FormatTrim(after["focusYear"])}.");
        }

        if (Changed(before, after, "literacyPercent"))
        {
            parts.Add($"Literacy moved from {FormatTrim(before["literacyPercent"])}% to {FormatTrim(after["literacyPercent"])}%.");
        }

        if (Changed(before, after, "pressFreedomIndex"))
        {
            parts.Add($"Press freedom moved from {FormatTrim(before["pressFreedomIndex"])} to {FormatTrim(after["pressFreedomIndex"])}.");
        }

        if (Changed(before, after, "nationalistMobilisation"))
        {
            parts.Add($"Mobilisation moved from {FormatTrim(before["nationalistMobilisation"])} to {FormatTrim(after["nationalistMobilisation"])}.");
        }

        if (Changed(before, after, "colonialRestrictiveness"))
        {
            parts.Add($"Restrictiveness moved from {FormatTrim(before["colonialRestrictiveness"])} to {FormatTrim(after["colonialRestrictiveness"])}.");
        }

        var previousReadiness = beforeOutcome.Find("readinessIndex")?.Value ?? 0d;
        var currentReadiness = afterOutcome.Find("readinessIndex")?.Value ?? 0d;
        parts.Add($"Readiness changed from {Format(previousReadiness)} to {Format(currentReadiness)}.");

        if (Changed(before, after, "nationalistMobilisation") && Changed(before, after, "pressFreedomIndex"))
        {
            parts.Add("Mobilisation and press move together here: parties need newspapers to reach readers, so their product drives movement momentum.");
        }

        parts.Add("This index is a teaching simplification, not a measured historical quantity.");
        return string.Join(" ", parts);
    }

    private static bool Changed(ParameterSet before, ParameterSet after, string name) =>
        before.TryGet(name, out var previous)
        && after.TryGet(name, out var current)
        && Math.Abs(current - previous) > ComparisonTolerance;
}
