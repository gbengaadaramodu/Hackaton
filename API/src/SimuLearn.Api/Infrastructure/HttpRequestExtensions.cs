using SimuLearn.Application.Contracts;

namespace SimuLearn.Api.Infrastructure;

public static class HttpRequestExtensions
{
    /// <summary>Parses bounded pagination from the query string; invalid values fall back to defaults.</summary>
    public static PageQuery ToPageQuery(this HttpRequest request)
    {
        var page = int.TryParse(request.Query["page"], out var parsedPage) ? parsedPage : 1;
        var pageSize = int.TryParse(request.Query["pageSize"], out var parsedSize) ? parsedSize : 20;
        return PageQuery.Create(page, pageSize);
    }

    public static string? CorrelationId(this HttpContext context) =>
        context.Items[CorrelationIdMiddleware.ItemKey] as string;

    public static string? ClientIp(this HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();
}
