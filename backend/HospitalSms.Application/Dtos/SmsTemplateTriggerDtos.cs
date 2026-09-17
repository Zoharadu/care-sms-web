namespace HospitalSms.Application.Dtos;

public sealed class CreateSmsTemplateTriggerRequest
{
    public int TemplateId { get; set; }
    public int TriggerId { get; set; }
}

public sealed record UpdateSmsTemplateTriggerRequest(int TriggerId);

public sealed class SmsTemplateTriggerResponse
{
    public int TtId { get; set; }
    public int TemplateId { get; set; }
    public int TriggerId { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateUser { get; set; }
}
