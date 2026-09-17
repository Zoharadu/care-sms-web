using HospitalSms.Admin.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Xunit;

namespace HospitalSms.Admin.Api.Tests.Controllers;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task UnauthorizedAccessException_ReturnsUnauthorizedInsteadOfServerError()
    {
        var handler = new GlobalExceptionHandler(new TestHostEnvironment());
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new UnauthorizedAccessException(
                "An authenticated Windows user is required."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            response.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "HospitalSms.Admin.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
