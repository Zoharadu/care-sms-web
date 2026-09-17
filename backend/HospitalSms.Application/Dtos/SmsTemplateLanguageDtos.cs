using System.ComponentModel.DataAnnotations;

namespace HospitalSms.Application.Dtos;

public sealed class CreateSmsTemplateLanguageRequest
{
    [Range(1, int.MaxValue)]
    public int TemplateId { get; set; }

    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string TemplateText { get; set; } = null!;

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateSmsTemplateLanguageRequest
{
    public int TemplateLanguageId { get; set; }

    [Range(1, int.MaxValue)]
    public int TemplateId { get; set; }

    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string TemplateText { get; set; } = null!;

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
