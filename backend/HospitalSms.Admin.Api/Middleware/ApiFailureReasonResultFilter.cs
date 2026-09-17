using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HospitalSms.Admin.Api.Middleware;

public sealed class ApiFailureReasonResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        if (ApiLogContext.GetFailureReason(context.HttpContext) is null)
        {
            var reason = ExtractFailureReason(context.Result);
            if (reason is not null)
            {
                ApiLogContext.SetFailureReason(context.HttpContext, reason);
            }
        }

        await next();
    }

    private static string? ExtractFailureReason(IActionResult result)
    {
        if (result is not ObjectResult { Value: ProblemDetails problemDetails })
        {
            return null;
        }

        if (problemDetails is ValidationProblemDetails validationProblemDetails
            && validationProblemDetails.Errors.Count > 0)
        {
            var invalidFields = string.Join(
                ", ",
                validationProblemDetails.Errors.Keys.OrderBy(field => field));

            return $"Request validation failed for fields: {invalidFields}.";
        }

        return !string.IsNullOrWhiteSpace(problemDetails.Detail)
            ? problemDetails.Detail
            : problemDetails.Title;
    }
}
