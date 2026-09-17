namespace HospitalSms.Application.Dtos;

public sealed class SmsTemplateDto
{
    public int TemplateId { get; set; }
    public string? TemplateCode { get; set; }
    public string TemplateName { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool IsEditable { get; set; }
    public int? VersionNumber { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateUser { get; set; }
}

public sealed class CreateSmsTemplateDto
{
    public string? TemplateCode { get; set; }
    public string TemplateName { get; set; } = null!;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool IsEditable { get; set; }
    public int? VersionNumber { get; set; }
}

public sealed class UpdateSmsTemplateDto
{
    public int TemplateId { get; set; }
    public string? TemplateCode { get; set; }
    public string TemplateName { get; set; } = null!;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool IsEditable { get; set; }
    public int? VersionNumber { get; set; }
}
