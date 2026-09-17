using HospitalSms.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IActiveDirectoryService _adService;

    public UserController(IActiveDirectoryService adService)
    {
        _adService = adService;
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetUserProfile()
    {
        string? windowsIdentity = User.Identity?.Name;

        if (string.IsNullOrEmpty(windowsIdentity))
        {
            this.SetFailureReason(
                "Windows authentication did not provide a user identity.");
            return Unauthorized("Could not retrieve Windows Identity. Ensure Windows Auth is enabled in IIS.");
        }

        var userDetails = _adService.GetUserFromAd(windowsIdentity);

        if (userDetails == null)
        {
            this.SetFailureReason(
                "The authenticated Windows user could not be resolved in Active Directory.");
            return NotFound($"User identity found ({windowsIdentity}), but could not look up details in Active Directory.");
        }

        return Ok(userDetails);
    }
}
