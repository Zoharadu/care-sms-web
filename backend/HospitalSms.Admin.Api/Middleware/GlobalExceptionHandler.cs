using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            BadHttpRequestException badRequestException => badRequestException.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode >= StatusCodes.Status500InternalServerError
                ? "An unexpected server error occurred."
                : "The request could not be processed.",
            Detail = _environment.IsDevelopment()
                ? exception.Message
                : statusCode >= StatusCodes.Status500InternalServerError
                    ? "The request could not be completed."
                    : "The request is invalid.",
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
