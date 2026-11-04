using SimuLearn.Domain.Common;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Simulations.Models;
using Xunit;

namespace SimuLearn.Tests;

public sealed class HistoryAndRenderTests
{
    private readonly NigerianIndependenceModel _history = new();

    [Fact]
    public void History_readiness_follows_published_weights()
    {
        var outcome = _history.Simulate(new ParameterSet(new Dictionary<string, double>
        {
            ["focusYear"] = 1950,
            ["literacyPercent"] = 35,
            ["pressFreedomIndex"] = 55,
            ["nationalistMobilisation"] = 60,
            ["colonialRestrictiveness"] = 40,
        }));

        var literacyScaled = 35d / 80d * 100d;
        var expected = Math.Clamp(0.45 * 60 + 0.30 * 55 + 0.25 * literacyScaled - 0.15 * 40 + (1950 - 1945) / 15d * 10, 0, 100);
        Assert.Equal(expected, outcome.Find("readinessIndex")!.Value, 4);
        Assert.Equal(33d, outcome.Find("movementMomentum")!.Value, 4);
        Assert.True(outcome.Trajectory.Count > 1);
    }

    [Fact]
    public void History_rejects_the_screenshot_value_38_5_for_focus_year()
    {
        var result = _history.Validate(new ParameterSet(new Dictionary<string, double>
        {
            ["focusYear"] = 38.5,
            ["literacyPercent"] = 35,
            ["pressFreedomIndex"] = 55,
            ["nationalistMobilisation"] = 60,
            ["colonialRestrictiveness"] = 40,
        }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Parameter == "focusYear" && i.Code == "out_of_range");
    }

    [Fact]
    public void History_validates_year_bounds_and_subject_is_history()
    {
        Assert.Equal("History", _history.Subject);
        Assert.Equal("svg-2d", _history.Renderer);
        var result = _history.Validate(new ParameterSet(new Dictionary<string, double> { ["focusYear"] = 1700 }));
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "out_of_range");
    }

    [Fact]
    public void Svg_renderer_frame_zero_differs_from_final_frame()
    {
        var state = _history.CreateInitialState();
        var outcome = _history.Simulate(state);

        var first = SvgSceneRenderer.Render(_history, state, outcome, 0);
        var last = SvgSceneRenderer.Render(_history, state, outcome, outcome.Trajectory.Count - 1);

        Assert.Contains("Focus year 1914", first);
        Assert.Contains("Focus year 1950", last);
        Assert.NotEqual(first, last);
    }

    [Fact]
    public void Svg_renderer_covers_every_model_without_leaking_markup()
    {
        AssertSvg(new InclinedPlaneModel());
        AssertSvg(new DcCircuitModel());
        AssertSvg(_history);
    }

    private static void AssertSvg(ISimulationModel model)
    {
        var state = model.CreateInitialState();
        var svg = SvgSceneRenderer.Render(model, state, model.Simulate(state));
        Assert.StartsWith("<svg", svg);
        Assert.Contains("viewBox", svg);
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", svg, StringComparison.OrdinalIgnoreCase);
    }
}
