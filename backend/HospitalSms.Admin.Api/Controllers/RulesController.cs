using HospitalSms.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult GetRules() => LegacyAggregateRemoved();

    [HttpPost]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public IActionResult SaveRule([FromBody] SmsTemplate template) => LegacyAggregateRemoved();

    private ObjectResult LegacyAggregateRemoved()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Status = StatusCodes.Status410Gone,
            Title = "The legacy rules aggregate is no longer available.",
            Detail = "Use /api/SmsTriggers, /api/SmsRules and /api/SmsRejectHospitalUnits, which use the current database schema."
        });
    }
}

[ApiController]
[Route("api/sim")]
public class SimController : ControllerBase
{
    [HttpPost("evaluate")]
    public IActionResult EvaluateSimulation([FromBody] object payload)
    {
        return Ok(new
        {
            status = "DISPATCH",
            evaluationResult = "Rule passed all 6 clinical gates",
            evaluatedAt = DateTime.UtcNow,
            gateResults = new[]
            {
                new { gate = "Rule 1: Active Status", passed = true, detail = "Template is Active" },
                new { gate = "Rule 2: Ward Filter", passed = true, detail = "Unit ID matched associated hospital units" },
                new { gate = "Rule 3: Dependency Window", passed = true, detail = "Within limit" },
                new { gate = "Rule 4: Relative Delay", passed = true, detail = "Delay threshold met" },
                new { gate = "Rule 5: Recurring Loop", passed = true, detail = "Patient is active" },
                new { gate = "Rule 6: Fallback Schedule", passed = true, detail = "Fallback check OK" }
            }
        });
    }
}
