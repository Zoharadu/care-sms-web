using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/sms")]
public sealed class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;

    public SmsController(ISmsService smsService)
    {
        _smsService = smsService;
    }

    [HttpGet("test-phones")]
    [ProducesResponseType(typeof(IReadOnlyList<SmsTestPhoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<SmsTestPhoneDto>>> GetTestPhones(
        CancellationToken cancellationToken)
    {
        var testPhones = await _smsService.GetActiveTestPhonesAsync(cancellationToken);
        return Ok(testPhones);
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(SendSmsResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SendSmsResponse>> SendSms(
        [FromBody] SendSmsRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null
            || request.CategoryId <= 0
            || request.TemplateId <= 0
            || string.IsNullOrWhiteSpace(request.LanguageId)
            || request.LanguageId.Length > SendSmsRequest.MaximumLanguageIdLength
            || string.IsNullOrWhiteSpace(request.PhoneNumber)
            || string.IsNullOrWhiteSpace(request.Message)
            || request.Message.Length > SendSmsRequest.MaximumMessageLength
            || !long.TryParse(
                request.PhoneNumber.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var phoneNumber)
            || phoneNumber <= 0)
        {
            return this.BadRequestWithReason(
                "The SMS request is invalid. CategoryId, TemplateId and phone number must be positive; language, message and phone number are required and must be within their allowed lengths.");
        }

        await _smsService.QueueSmsAsync(
            request.CategoryId,
            request.TemplateId,
            request.LanguageId,
            request.Message,
            phoneNumber,
            cancellationToken);

        return Accepted(new SendSmsResponse { Success = true });
    }
}
