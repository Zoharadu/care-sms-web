using HospitalSms.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsTemplateHospitalUnitsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult<IEnumerable<SmsTemplateHospitalUnit>> GetTemplateHospitalUnits(
        [FromQuery] int? templateId,
        [FromQuery] int? hospitalId) => RemovedResource();

    [HttpPost]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult<SmsTemplateHospitalUnit> CreateAssignment(
        [FromBody] SmsTemplateHospitalUnit unitAssignment) => RemovedResource();

    [HttpDelete("template/{templateId:int}/hospital/{hospitalId:int}/unit/{unitId:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult DeleteAssignment(int templateId, int hospitalId, int unitId) =>
        RemovedResource();

    private ObjectResult RemovedResource()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "The template hospital-unit resource no longer exists.",
            Detail = "dbo.sms_template_hospital_unit is not present. Use /api/SmsRejectHospitalUnits for template exclusions."
        });
    }
}
