using System.ComponentModel.DataAnnotations;

namespace HospitalSms.Application.Dtos;

public sealed class SendSmsRequest
{
    public const int MaximumMessageLength = 1000;
    public const int MaximumLanguageIdLength = 2;

    [Required]
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int TemplateId { get; set; }

    [Required]
    [StringLength(MaximumLanguageIdLength)]
    public string LanguageId { get; set; } = string.Empty;

    [Required]
    [StringLength(MaximumMessageLength)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed class SendSmsResponse
{
    public bool Success { get; set; }
}

public sealed class SmsTestPhoneDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
