using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalSms.Infrastructure.Data;
using HospitalSms.Domain.Entities;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HospitalsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public HospitalsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmsHospital>>> GetHospitals()
    {
        var hospitals = await _context.SmsHospitals
            .Where(x => x.IsActive)
            .ToListAsync();

        return Ok(hospitals);
    }

    [HttpPost]
    public async Task<ActionResult<SmsHospital>> PostHospital(SmsHospital hospital)
    {
        hospital.CreateDate = DateTime.Now;
        _context.SmsHospitals.Add(hospital);
        await _context.SaveChangesAsync();
        return Ok(hospital);
    }
}
