using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics;

namespace HospitalSms.Admin.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var traceId = context.TraceIdentifier;
        using var requestScope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["TraceId"] = traceId
            });

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            LogUnhandledFailure(context, exception, startedAt, traceId);
            throw;
        }

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var operation = GetOperationName(context, exceptionFeature?.Endpoint);

        if (exceptionFeature?.Error is { } handledException)
        {
            _logger.LogError(
                handledException,
                "API request failed. TraceId: {TraceId}, Operation: {Operation}, Method: {Method}, StatusCode: {StatusCode}, ExceptionType: {ExceptionType}, FailureReason: {FailureReason}, ElapsedMilliseconds: {ElapsedMilliseconds:F2}.",
                traceId,
                operation,
                context.Request.Method,
                context.Response.StatusCode,
                handledException.GetType().Name,
                handledException.Message,
                elapsedMilliseconds);
            return;
        }

        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            var failureReason = ApiLogContext.GetFailureReason(context)
                ?? ReasonPhrases.GetReasonPhrase(context.Response.StatusCode);
            var logLevel = context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : LogLevel.Warning;

            _logger.Log(
                logLevel,
                "API request failed. TraceId: {TraceId}, Operation: {Operation}, Method: {Method}, StatusCode: {StatusCode}, FailureReason: {FailureReason}, ElapsedMilliseconds: {ElapsedMilliseconds:F2}.",
                traceId,
                operation,
                context.Request.Method,
                context.Response.StatusCode,
                string.IsNullOrWhiteSpace(failureReason) ? "The API returned a failure status code." : failureReason,
                elapsedMilliseconds);
            return;
        }

        _logger.LogInformation(
            "API request succeeded. TraceId: {TraceId}, Operation: {Operation}, Method: {Method}, StatusCode: {StatusCode}, ElapsedMilliseconds: {ElapsedMilliseconds:F2}.",
            traceId,
            operation,
            context.Request.Method,
            context.Response.StatusCode,
            elapsedMilliseconds);
    }

    private void LogUnhandledFailure(
        HttpContext context,
        Exception exception,
        long startedAt,
        string traceId)
    {
        _logger.LogError(
            exception,
            "API request failed before an error response could be created. TraceId: {TraceId}, Operation: {Operation}, Method: {Method}, ExceptionType: {ExceptionType}, FailureReason: {FailureReason}, ElapsedMilliseconds: {ElapsedMilliseconds:F2}.",
            traceId,
            GetOperationName(context),
            context.Request.Method,
            exception.GetType().Name,
            exception.Message,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    }

    private static string GetOperationName(HttpContext context, Endpoint? endpoint = null)
    {
        endpoint ??= context.GetEndpoint();
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        return action is null
            ? endpoint?.DisplayName ?? context.Request.Path.Value ?? "unknown"
            : $"{action.ControllerName}.{action.ActionName}";
    }
}
