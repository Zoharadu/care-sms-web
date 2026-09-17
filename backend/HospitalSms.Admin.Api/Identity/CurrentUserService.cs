using HospitalSms.Application.Abstractions;

namespace HospitalSms.Admin.Api.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private const int MaximumUserNameLength = 50;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetRequiredUserName()
    {
        var identity = _httpContextAccessor.HttpContext?.User?.Identity;
        if (identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(identity.Name))
        {
            throw new UnauthorizedAccessException(
                "An authenticated Windows user is required.");
        }

        if (identity.Name.Length > MaximumUserNameLength)
        {
            throw new InvalidOperationException(
                $"The authenticated Windows user name exceeds the supported {MaximumUserNameLength}-character audit length.");
        }

        return identity.Name;
    }
}
