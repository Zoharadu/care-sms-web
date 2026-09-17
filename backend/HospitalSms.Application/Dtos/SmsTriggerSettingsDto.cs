namespace HospitalSms.Application.Dtos;

public sealed class SmsTriggerSettingsDto
{
    public int? RuleId { get; set; }
    public int TtId { get; set; }
    public int TemplateId { get; set; }
    public string? TemplateUpdateUser { get; set; }
    public int TriggerId { get; set; }
    public string TriggerCode { get; set; } = null!;
    public string TriggerName { get; set; } = null!;
    public string? TriggerDescription { get; set; }
    public bool TriggerIsActive { get; set; }
    public string? TemplateTriggerUpdateUser { get; set; }
    public int? DependOnTtId { get; set; }
    public double? DependencyMaxMinutes { get; set; }
    public double? MaxTimeTokefInMinutes { get; set; }
    public bool? IsConstant { get; set; }
    public string? StartTimeRange { get; set; }
    public string? EndTimeRange { get; set; }
    public bool? IsRecurring { get; set; }
    public int? RecurringIntervalDays { get; set; }
    public string? RecurringTimeOfDay { get; set; }
    public string? RecurringStopCondition { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateUser { get; set; }
}
