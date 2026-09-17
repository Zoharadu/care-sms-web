using HospitalSms.Admin.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Controllers;

internal static class ControllerFailureExtensions
{
    public static BadRequestResult BadRequestWithReason(
        this ControllerBase controller,
        string reason)
    {
        controller.SetFailureReason(reason);
        return controller.BadRequest();
    }

    public static NotFoundResult NotFoundWithReason(
        this ControllerBase controller,
        string reason)
    {
        controller.SetFailureReason(reason);
        return controller.NotFound();
    }

    public static void SetFailureReason(
        this ControllerBase controller,
        string reason)
    {
        ApiLogContext.SetFailureReason(
            controller.ControllerContext.HttpContext,
            reason);
    }
}
