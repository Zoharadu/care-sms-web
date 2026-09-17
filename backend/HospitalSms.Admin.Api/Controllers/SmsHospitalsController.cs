using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsHospitalsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsHospitalsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmsHospital>>> GetHospitals()
    {
        var hospitals = await _context.SmsHospitals
            .OrderBy(h => h.HospitalId)
            .ToListAsync();

        return Ok(hospitals);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SmsHospital>> GetHospital(int id)
    {
        var hospital = await _context.SmsHospitals.FindAsync(id);

        if (hospital == null)
        {
            return this.NotFoundWithReason("The requested SMS hospital does not exist.");
        }

        return Ok(hospital);
    }

    [HttpPost]
    public async Task<ActionResult<SmsHospital>> CreateHospital(SmsHospital hospital)
    {
        hospital.CreateDate = DateTime.Now;
        _context.SmsHospitals.Add(hospital);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHospital), new { id = hospital.HospitalId }, hospital);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHospital(int id, SmsHospital hospital)
    {
        if (id != hospital.HospitalId)
        {
            return this.BadRequestWithReason(
                "The route hospital ID does not match the request-body hospital ID.");
        }

        var existing = await _context.SmsHospitals.FindAsync(id);
        if (existing == null)
        {
            return this.NotFoundWithReason("The SMS hospital to update does not exist.");
        }

        existing.HospitalTypeId = hospital.HospitalTypeId;
        existing.HospitalDescription = hospital.HospitalDescription;
        existing.HospitalName = hospital.HospitalName;
        existing.IsActive = hospital.IsActive;
        existing.UpdateDate = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!HospitalExists(id))
            {
                return this.NotFoundWithReason(
                    "The SMS hospital was removed by another operation before the update completed.");
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHospital(int id)
    {
        var hospital = await _context.SmsHospitals.FindAsync(id);
        if (hospital == null)
        {
            return this.NotFoundWithReason("The SMS hospital to delete does not exist.");
        }

        _context.SmsHospitals.Remove(hospital);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool HospitalExists(int id)
    {
        return _context.SmsHospitals.Any(e => e.HospitalId == id);
    }
}
