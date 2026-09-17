using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HospitalSmsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var html = "<h1>HospitalSms</h1>";
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }
    }
}
