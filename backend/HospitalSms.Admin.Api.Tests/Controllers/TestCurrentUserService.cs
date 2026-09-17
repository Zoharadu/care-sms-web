using HospitalSms.Application.Abstractions;

namespace HospitalSms.Admin.Api.Tests.Controllers;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public const string DefaultUserName = @"EXAMPLE\test.user";
    private readonly string? _userName;

    public TestCurrentUserService(string? userName = DefaultUserName)
    {
        _userName = userName;
    }

    public string GetRequiredUserName()
    {
        if (string.IsNullOrWhiteSpace(_userName))
        {
            throw new UnauthorizedAccessException(
                "An authenticated Windows user is required.");
        }

        return _userName;
    }
}
