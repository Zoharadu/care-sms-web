using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsCategoryStepsController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;

    public SmsCategoryStepsController(HospitalSmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmsCategoryStep>>> GetCategorySteps([FromQuery] int? categoryId)
    {
        var query = _context.SmsCategorySteps.AsQueryable();
        if (categoryId.HasValue)
        {
            query = query.Where(s => s.CategoryId == categoryId.Value);
        }

        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SmsCategoryStep>> GetCategoryStep(int id)
    {
        var step = await _context.SmsCategorySteps.FindAsync(id);
        if (step == null)
        {
            return this.NotFoundWithReason("The requested SMS category step does not exist.");
        }
        return Ok(step);
    }

    [HttpPost]
    public async Task<ActionResult<SmsCategoryStep>> CreateCategoryStep(SmsCategoryStep step)
    {
        step.CreateDate = DateTime.Now;
        _context.SmsCategorySteps.Add(step);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCategoryStep), new { id = step.CategoryStepId }, step);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategoryStep(int id, SmsCategoryStep step)
    {
        if (id != step.CategoryStepId)
        {
            return this.BadRequestWithReason(
                "The route category-step ID does not match the request-body category-step ID.");
        }

        var existing = await _context.SmsCategorySteps.FindAsync(id);
        if (existing == null)
        {
            return this.NotFoundWithReason("The SMS category step to update does not exist.");
        }

        existing.CategoryId = step.CategoryId;
        existing.TemplateId = step.TemplateId;
        existing.DependentatId = step.DependentatId;
        existing.DelayInMinutes = step.DelayInMinutes;
        existing.IsActive = step.IsActive;
        existing.IsEditable = step.IsEditable;
        existing.UpdateDate = DateTime.Now;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategoryStep(int id)
    {
        var step = await _context.SmsCategorySteps.FindAsync(id);
        if (step == null)
        {
            return this.NotFoundWithReason("The SMS category step to delete does not exist.");
        }

        _context.SmsCategorySteps.Remove(step);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
