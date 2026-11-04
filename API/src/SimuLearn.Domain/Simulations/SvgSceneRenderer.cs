using System.Globalization;
using System.Text;

namespace SimuLearn.Domain.Simulations;

/// <summary>
/// Server-painted SVG for the svg-2d renderer. Only numbers from the validated state and
/// outcome are written into the markup; no learner text ever reaches the SVG.
/// </summary>
public static class SvgSceneRenderer
{
    public static string Render(ISimulationModel model, ParameterSet state, SimulationOutcome outcome)
    {
        return model.ModelKey switch
        {
            Models.InclinedPlaneModel.Key => RenderIncline(model, state, outcome),
            Models.DcCircuitModel.Key => RenderCircuit(model, state, outcome),
            Models.NigerianIndependenceModel.Key => RenderIndependence(model, state, outcome),
            _ => RenderGeneric(model, outcome),
        };
    }

    private static string RenderIncline(ISimulationModel model, ParameterSet state, SimulationOutcome outcome)
    {
        var angle = state.TryGet("angleDegrees", out var a) ? a : 20;
        var length = state.TryGet("rampLengthM", out var l) ? Math.Max(l, 0.5) : 4;
        var accel = outcome.Find("acceleration")?.Value ?? 0;
        var last = outcome.Trajectory.Count > 0 ? outcome.Trajectory[^1] : new TrajectoryPoint(0, 0, 0);
        var progress = Math.Clamp(last.PositionMetres / length, 0, 1);
        var radians = angle * Math.PI / 180;
        var rampW = 380d;
        var rampH = rampW * Math.Tan(radians);
        var baseY = 290d;
        var topX = 110d;
        var topY = baseY - rampH;
        var blockX = topX + (rampW - 40) * progress;
        var blockY = topY + rampH * progress - 26;
        var moves = outcome.IsInMotion ? "moving" : "at rest";

        var svg = new StringBuilder();
        svg.Append(SvgOpen(model));
        svg.Append($"<polygon points=\"90,{F(baseY)} 490,{F(baseY)} {F(topX)},{F(topY)}\" fill=\"#e2e8f0\" stroke=\"#64748b\"/>");
        svg.Append($"<rect x=\"{F(blockX)}\" y=\"{F(blockY)}\" width=\"34\" height=\"24\" rx=\"3\" fill=\"#047857\"/>");
        svg.Append($"<line x1=\"{F(blockX + 17)}\" y1=\"{F(blockY + 24)}\" x2=\"{F(blockX + 42)}\" y2=\"{F(blockY + 24)}\" stroke=\"#b45309\" stroke-width=\"3\"/>");
        svg.Append(Text(90, 325, $"Angle {F1(angle)} deg, length {F1(length)} m, a = {F2(accel)} m/s2 ({moves})"));
        svg.Append(Text(90, 60, model.Title));
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string RenderCircuit(ISimulationModel model, ParameterSet state, SimulationOutcome outcome)
    {
        var voltage = state.TryGet("voltageV", out var v) ? v : 12;
        var resistance = state.TryGet("resistanceOhm", out var r) ? Math.Max(r, 0.5) : 6;
        var current = outcome.Find("current")?.Value ?? 0;
        var power = outcome.Find("power")?.Value ?? 0;
        var glow = Math.Clamp(current / 4, 0.15, 1);
        var op = glow.ToString("0.00", CultureInfo.InvariantCulture);

        var svg = new StringBuilder();
        svg.Append(SvgOpen(model));
        svg.Append("<rect x=\"90\" y=\"90\" width=\"460\" height=\"170\" fill=\"none\" stroke=\"#334155\" stroke-width=\"4\"/>");
        svg.Append("<line x1=\"90\" y1=\"150\" x2=\"90\" y2=\"200\" stroke=\"#b45309\" stroke-width=\"6\"/>");
        svg.Append("<line x1=\"250\" y1=\"90\" x2=\"390\" y2=\"90\" stroke=\"#047857\" stroke-width=\"8\" stroke-dasharray=\"14 10\"/>");
        svg.Append($"<circle cx=\"470\" cy=\"175\" r=\"26\" fill=\"#fef3c7\" stroke=\"#b45309\" opacity=\"{op}\"/>");
        svg.Append(Text(430, 180, $"{F1(current)} A"));
        svg.Append(Text(90, 60, model.Title));
        svg.Append(Text(90, 300, $"Source {F1(voltage)} V, resistor {F1(resistance)} ohm, power {F1(power)} W"));
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string RenderIndependence(ISimulationModel model, ParameterSet state, SimulationOutcome outcome)
    {
        var year = state.TryGet("focusYear", out var y) ? y : 1950;
        var readiness = outcome.Find("readinessIndex")?.Value ?? 0;
        var momentum = outcome.Find("movementMomentum")?.Value ?? 0;
        var x = 90 + (year - 1914) / 46d * 460;
        var readW = readiness / 100 * 460;
        var momW = momentum / 100 * 460;

        var svg = new StringBuilder();
        svg.Append(SvgOpen(model));
        svg.Append("<line x1=\"90\" y1=\"190\" x2=\"550\" y2=\"190\" stroke=\"#475569\" stroke-width=\"3\"/>");
        svg.Append(Marker(90, "1914"));
        svg.Append(Marker(350, "1945"));
        svg.Append(Marker(550, "1960"));
        svg.Append($"<circle cx=\"{F(x)}\" cy=\"190\" r=\"11\" fill=\"#047857\" stroke=\"#ffffff\" stroke-width=\"2\"/>");
        svg.Append(Bar(90, 120, readW, "#047857", $"Readiness {F1(readiness)}"));
        svg.Append(Bar(90, 150, momW, "#b45309", $"Momentum {F1(momentum)}"));
        svg.Append(Text(90, 60, model.Title));
        svg.Append(Text(90, 90, $"Focus year {F0(year)}"));
        svg.Append(Text(90, 300, "1914 amalgamation to 1960 independence. Index is a teaching aid."));
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string RenderGeneric(ISimulationModel model, SimulationOutcome outcome)
    {
        var svg = new StringBuilder();
        svg.Append(SvgOpen(model));
        svg.Append(Text(90, 60, model.Title));
        var y = 110d;
        foreach (var obs in outcome.Observations.Take(4))
        {
            var w = Math.Clamp(Math.Abs(obs.Value), 0, 460);
            svg.Append(Bar(90, y, w, "#047857", $"{Escape(obs.Label)} {F2(obs.Value)} {Escape(obs.Unit)}"));
            y += 30;
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string SvgOpen(ISimulationModel model) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 360\" role=\"img\" aria-label=\"{Escape(model.Title)}\">"
        + "<rect x=\"0\" y=\"0\" width=\"640\" height=\"360\" fill=\"#f8fafc\"/>";

    private static string Marker(double x, string label) =>
        $"<circle cx=\"{F(x)}\" cy=\"190\" r=\"6\" fill=\"#334155\"/>" + Text(x - 18, 215, label);

    private static string Bar(double x, double y, double width, string fill, string label) =>
        $"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(Math.Clamp(width, 0, 460))}\" height=\"18\" fill=\"{fill}\"/>"
        + Text(x + 4, y - 6, label);

    private static string Text(double x, double y, string value) =>
        $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-family=\"sans-serif\" font-size=\"15\" fill=\"#1e293b\">{Escape(value)}</text>";

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
             .Replace("<", "&lt;", StringComparison.Ordinal)
             .Replace(">", "&gt;", StringComparison.Ordinal)
             .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string F(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    private static string F0(double value) => value.ToString("0", CultureInfo.InvariantCulture);
    private static string F1(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    private static string F2(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
