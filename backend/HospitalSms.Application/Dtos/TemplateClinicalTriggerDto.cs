namespace HospitalSms.Application.Dtos;

public sealed class TemplateClinicalTriggerDto
{
    public int TtId { get; set; }
    public int TemplateId { get; set; }
    public int TriggerId { get; set; }
    public string TriggerName { get; set; } = null!;
    public string TriggerCode { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? UpdateUser { get; set; }
}
