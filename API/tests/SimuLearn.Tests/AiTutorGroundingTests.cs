using Microsoft.Extensions.Logging.Abstractions;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Ai;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Simulations.Models;
using SimuLearn.Domain.Tutoring;
using Xunit;

namespace SimuLearn.Tests;

public sealed class AiTutorGroundingTests
{
    private sealed class StubClient(string content) : IAiChatClient
    {
        public bool IsConfigured => true;
        public string ProviderName => "stub";
        public Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new AiChatResponse(content, "stub", "stub-model"));
    }

    [Fact]
    public async Task Tutor_accepts_answers_quoting_published_weights_and_bounds()
    {
        var model = new NigerianIndependenceModel();
        var state = model.CreateInitialState();
        var outcome = model.Simulate(state);

        var narrator = new AiTutorNarrator(
            new StubClient("Readiness uses 0.45 for mobilisation and covers 1914 to 1960, reaching 100 at most."),
            new DeterministicTutorNarrator(),
            NullLogger<AiTutorNarrator>.Instance);

        var reply = await narrator.AnswerAsync(
            "How is readiness calculated?",
            ExplanationLevel.Standard,
            new TutorContext { Model = model, State = state, Outcome = outcome },
            CancellationToken.None);

        Assert.False(reply.UsedFallback);
        Assert.Equal("stub", reply.Provider);
    }

    [Fact]
    public async Task Tutor_still_rejects_invented_measurements()
    {
        var model = new NigerianIndependenceModel();
        var state = model.CreateInitialState();
        var outcome = model.Simulate(state);

        var narrator = new AiTutorNarrator(
            new StubClient("The secret measurement is 999 amperes."),
            new DeterministicTutorNarrator(),
            NullLogger<AiTutorNarrator>.Instance);

        var reply = await narrator.AnswerAsync(
            "What is the secret measurement?",
            ExplanationLevel.Standard,
            new TutorContext { Model = model, State = state, Outcome = outcome },
            CancellationToken.None);

        Assert.True(reply.UsedFallback);
    }
}
