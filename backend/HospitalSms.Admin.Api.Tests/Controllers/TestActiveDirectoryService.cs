using HospitalSms.Application.Abstractions;
using HospitalSms.Application.Dtos;

namespace HospitalSms.Admin.Api.Tests.Controllers;

internal sealed class TestActiveDirectoryService : IActiveDirectoryService
{
    public UserDetails GetUserFromAd(string rawIdentityString) => new()
    {
        FullName = rawIdentityString,
        FirstName = "Test",
        LastName = "User",
        Email = "test.user@example.invalid"
    };
}
