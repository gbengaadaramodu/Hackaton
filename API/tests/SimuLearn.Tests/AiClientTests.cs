using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimuLearn.Application.Abstractions;
using SimuLearn.Infrastructure.Ai;
using Xunit;

namespace SimuLearn.Tests;

public sealed class AiClientTests
{
    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    [Fact]
    public async Task Meta_ai_format_sends_responses_payload_and_parses_output()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new DelegatingHandlerStub(req =>
        {
            capturedRequest = req;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();

            var metaResponseJson = """
            {
              "id": "resp_test123",
              "object": "response",
              "status": "completed",
              "model": "muse-spark-1.3",
              "output": [
                {
                  "type": "reasoning",
                  "summary": "Thinking about the simulation selection..."
                },
                {
                  "type": "message",
                  "role": "assistant",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"status\":\"ready\",\"modelKey\":\"dc-circuit\",\"confidence\":0.95}"
                    }
                  ]
                }
              ]
            }
            """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(metaResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var options = Options.Create(new AiOptions
        {
            Enabled = true,
            BaseUrl = "https://api.meta.ai/v1",
            ApiKey = "test-meta-key",
            Model = "muse-spark-1.3",
            ProviderName = "meta"
        });

        using var httpClient = new HttpClient(handler);
        using var client = new OpenAiCompatibleChatClient(
            httpClient,
            options,
            NullLogger<OpenAiCompatibleChatClient>.Instance);

        Assert.True(client.IsConfigured);
        Assert.Equal("meta", client.ProviderName);

        var result = await client.CompleteAsync(
            new AiChatRequest(
                SystemPrompt: "Select a simulation.",
                UserPrompt: "Simulate a resistor and battery circuit",
                Temperature: 0.2),
            CancellationToken.None);

        Assert.Equal("{\"status\":\"ready\",\"modelKey\":\"dc-circuit\",\"confidence\":0.95}", result.Content);
        Assert.Equal("muse-spark-1.3", result.Model);
        Assert.Equal("meta", result.Provider);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://api.meta.ai/v1/responses", capturedRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-meta-key", capturedRequest.Headers.Authorization?.Parameter);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;
        Assert.Equal("muse-spark-1.3", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal("Select a simulation.", root.GetProperty("instructions").GetString());

        var input = root.GetProperty("input");
        Assert.Equal(1, input.GetArrayLength());
        Assert.Equal("user", input[0].GetProperty("role").GetString());
        Assert.Equal("input_text", input[0].GetProperty("content")[0].GetProperty("type").GetString());
    }

    [Fact]
    public void EffectiveApiKey_falls_back_to_environment_variable()
    {
        Environment.SetEnvironmentVariable("MODEL_API_KEY", "env-secret-123");
        try
        {
            var options = new AiOptions
            {
                Enabled = true,
                BaseUrl = "https://api.meta.ai/v1",
                ApiKey = "",
                Model = "muse-wonder-1.0"
            };

            Assert.True(options.IsConfigured);
            Assert.Equal("env-secret-123", options.EffectiveApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MODEL_API_KEY", null);
        }
    }
}
