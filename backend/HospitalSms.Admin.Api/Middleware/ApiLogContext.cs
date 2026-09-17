namespace HospitalSms.Admin.Api.Middleware;

internal static class ApiLogContext
{
    private const string FailureReasonKey = "HospitalSms.ApiFailureReason";

    public static void SetFailureReason(HttpContext? context, string reason)
    {
        if (context is null || string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        context.Items[FailureReasonKey] = reason;
    }

    public static string? GetFailureReason(HttpContext context)
    {
        return context.Items.TryGetValue(FailureReasonKey, out var value)
            ? value as string
            : null;
    }
}
