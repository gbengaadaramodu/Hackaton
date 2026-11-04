using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Common;
using SimuLearn.Infrastructure.Ai;
using Xunit;

namespace SimuLearn.Tests;

public sealed class AiImageClientTests
{
    private sealed class HandlerStub(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    [Fact]
    public async Task Image_client_posts_configured_endpoint_and_parses_url()
    {
        string? uri = null;
        var handler = new HandlerStub(req =>
        {
            uri = req.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"model":"img-1","data":[{"url":"https://example.test/scene.png"}]}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });

        var options = Options.Create(new AiOptions
        {
            Enabled = true,
            ImageEnabled = true,
            ImageModel = "img-1",
            BaseUrl = "https://example.test/v1",
            ApiKey = "test-key",
            ProviderName = "test",
        });

        var client = new OpenAiCompatibleImageClient(
            new HttpClient(handler), options, NullLogger<OpenAiCompatibleImageClient>.Instance);

        Assert.True(client.IsConfigured);
        var result = await client.GenerateAsync(new AiImageRequest("A ramp."), CancellationToken.None);
        Assert.Equal("https://example.test/v1/images/generations", uri);
        Assert.Equal("https://example.test/scene.png", result.ImageUrl);
    }

    [Fact]
    public async Task Image_client_throws_when_not_configured()
    {
        var options = Options.Create(new AiOptions { Enabled = true, ImageEnabled = false });
        var client = new OpenAiCompatibleImageClient(
            new HttpClient(new HandlerStub(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            options,
            NullLogger<OpenAiCompatibleImageClient>.Instance);

        Assert.False(client.IsConfigured);
        await Assert.ThrowsAsync<GenerationFailedException>(
            () => client.GenerateAsync(new AiImageRequest("A ramp."), CancellationToken.None));
    }
}
