using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/preview")]
public class PreviewController : ControllerBase
{
    [HttpPost("render")]
    public IActionResult RenderPreview([FromBody] RenderRequest request)
    {
        if (string.IsNullOrEmpty(request.Body))
        {
            return Ok(new
            {
                renderedBody = "",
                charCount = 0,
                smsSegments = 0,
                placeholdersFound = new string[] { }
            });
        }

        bool isUnicode = Regex.IsMatch(request.Body, @"[\u0590-\u05FF\u0600-\u06FF]");
        int charCount = request.Body.Length;
        int smsSegments = 0;

        if (isUnicode)
        {
            smsSegments = charCount <= 70 ? 1 : (int)Math.Ceiling(charCount / 67.0);
        }
        else
        {
            smsSegments = charCount <= 160 ? 1 : (int)Math.Ceiling(charCount / 153.0);
        }

        var matches = Regex.Matches(request.Body, @"\{([^}]+)\}")
                           .Select(m => m.Value)
                           .ToArray();

        return Ok(new
        {
            renderedBody = request.Body,
            charCount,
            smsSegments,
            placeholdersFound = matches
        });
    }

    [HttpPost("test-send")]
    public IActionResult TestSend()
    {
        return Ok(new { status = "success", messageId = "dotnet-msg-" + DateTime.Now.Ticks });
    }

    public class RenderRequest
    {
        public string Body { get; set; } = null!;
        public string? LanguageCode { get; set; }
    }
}
