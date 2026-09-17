using HospitalSms.Admin.Api.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetRequiredUserName_AuthenticatedIdentity_ReturnsFullDomainUser()
    {
        var service = CreateService(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, @"EXAMPLE\test.user") },
                TestAuthenticationHandler.SchemeName));

        var userName = service.GetRequiredUserName();

        Assert.Equal(@"EXAMPLE\test.user", userName);
    }

    [Fact]
    public void GetRequiredUserName_AnonymousIdentity_ThrowsUnauthorizedAccessException()
    {
        var service = CreateService(new ClaimsIdentity());

        Assert.Throws<UnauthorizedAccessException>(service.GetRequiredUserName);
    }

    [Fact]
    public void GetRequiredUserName_NameExceedsColumnLength_ThrowsWithoutTruncating()
    {
        var longUserName = $@"EXAMPLE\{new string('a', 44)}";
        var service = CreateService(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, longUserName) },
                TestAuthenticationHandler.SchemeName));

        var exception = Assert.Throws<InvalidOperationException>(
            service.GetRequiredUserName);

        Assert.Contains("50-character", exception.Message);
    }

    private static CurrentUserService CreateService(ClaimsIdentity identity)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        return new CurrentUserService(accessor);
    }
}
