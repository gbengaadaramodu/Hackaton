using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimuLearn.Domain.Common;

namespace SimuLearn.Api.Infrastructure;

/// <summary>
/// Maps expected domain failures onto RFC 7807 responses with a stable machine-readable code.
/// Unexpected failures are logged with the correlation id and never expose internals.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Method} {Path} was cancelled by the client.",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            await WriteAsync(context, exception);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError(exception, "Unhandled exception after the response started.");
            return;
        }

        var (status, title) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Invalid request"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            UnsupportedRequestException => (StatusCodes.Status422UnprocessableEntity, "Unsupported request"),
            GenerationFailedException => (StatusCodes.Status502BadGateway, "Generation unavailable"),
            DomainException => (StatusCodes.Status400BadRequest, "Request rejected"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled error for {Method} {Path} ({TraceId}).",
                context.Request.Method, context.Request.Path, traceId);
        }
        else
        {
            logger.LogInformation("{Code} for {Method} {Path}: {Message}",
                (exception as DomainException)?.Code ?? "error",
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "The server could not complete the request. Quote the trace id if you report this."
                : exception.Message,
            Instance = context.Request.Path,
        };

        problem.Extensions["traceId"] = traceId;

        if (exception is BadHttpRequestException)
        {
            problem.Detail = "The request body could not be read. Check the JSON payload and value formats.";
            problem.Extensions["code"] = "invalid_request";
        }

        if (exception is DomainException domainException)
        {
            problem.Extensions["code"] = domainException.Code;
        }

        if (exception is DomainValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors;
        }

        if (exception is UnsupportedRequestException unsupported && unsupported.ClarifyingQuestions.Count > 0)
        {
            problem.Extensions["clarifyingQuestions"] = unsupported.ClarifyingQuestions;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, ApiJson.Options);
    }
}
