using HospitalSms.Domain.Entities;
using HospitalSms.Application.Dtos;
using HospitalSms.Application.Abstractions;
using HospitalSms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalSms.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmsTemplateLanguagesController : ControllerBase
{
    private readonly HospitalSmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SmsTemplateLanguagesController(
        HospitalSmsDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmsTemplateLanguage>>> GetTemplateLanguages([FromQuery] int? templateId)
    {
        var query = _context.SmsTemplateLanguages.AsQueryable();
        if (templateId.HasValue)
        {
            query = query.Where(l => l.TemplateId == templateId.Value);
        }

        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SmsTemplateLanguage>> GetTemplateLanguage(int id)
    {
        var lang = await _context.SmsTemplateLanguages.FindAsync(id);
        if (lang == null)
        {
            return this.NotFoundWithReason("The requested SMS template language does not exist.");
        }
        return Ok(lang);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SmsTemplateLanguage>> CreateTemplateLanguage(
        [FromBody] CreateSmsTemplateLanguageRequest request)
    {
        var currentUser = _currentUserService.GetRequiredUserName();

        var lang = new SmsTemplateLanguage
        {
            TemplateId = request.TemplateId,
            LanguageCode = request.LanguageCode,
            TemplateText = request.TemplateText,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreateDate = DateTime.Now,
            UpdateUser = currentUser
        };

        _context.SmsTemplateLanguages.Add(lang);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTemplateLanguage), new { id = lang.TemplateLanguageId }, lang);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTemplateLanguage(
        int id,
        [FromBody] UpdateSmsTemplateLanguageRequest request)
    {
        if (id != request.TemplateLanguageId)
        {
            return this.BadRequestWithReason(
                "The route template-language ID does not match the request-body template-language ID.");
        }

        var currentUser = _currentUserService.GetRequiredUserName();

        var existing = await _context.SmsTemplateLanguages.FindAsync(id);
        if (existing == null)
        {
            return this.NotFoundWithReason("The SMS template language to update does not exist.");
        }

        existing.TemplateId = request.TemplateId;
        existing.LanguageCode = request.LanguageCode;
        existing.TemplateText = request.TemplateText;
        existing.IsDefault = request.IsDefault;
        existing.IsActive = request.IsActive;
        existing.UpdateDate = DateTime.Now;
        existing.UpdateUser = currentUser;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplateLanguage(int id)
    {
        var lang = await _context.SmsTemplateLanguages.FindAsync(id);
        if (lang == null)
        {
            return this.NotFoundWithReason("The SMS template language to delete does not exist.");
        }

        _context.SmsTemplateLanguages.Remove(lang);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
