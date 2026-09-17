namespace HospitalSms.Application.Dtos;

public sealed class SmsRuleHierarchyDto
{
    public int ProjectId { get; set; }
    public SmsRuleCategoryDetailsDto Category { get; set; } = null!;
    public SmsRuleTemplateDetailsDto Template { get; set; } = null!;
    public SmsRuleTemplateTriggerDetailsDto TemplateTrigger { get; set; } = null!;
    public SmsRuleResponse? Rule { get; set; }
    public List<ExcludedHospitalUnitDto> ExcludedHospitalUnits { get; set; } = [];
}

public sealed class SmsRuleCategoryDetailsDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsEditable { get; set; }
}

public sealed class SmsRuleTemplateDetailsDto
{
    public int TemplateId { get; set; }
    public string? TemplateCode { get; set; }
    public string TemplateName { get; set; } = null!;
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool IsEditable { get; set; }
    public int? VersionNumber { get; set; }
    public string? UpdateUser { get; set; }
}

public sealed class SmsRuleTemplateTriggerDetailsDto
{
    public int TtId { get; set; }
    public int TriggerId { get; set; }
    public string TriggerCode { get; set; } = null!;
    public string TriggerName { get; set; } = null!;
    public string? TriggerDescription { get; set; }
    public bool TriggerIsActive { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateUser { get; set; }
}

public sealed class ExcludedHospitalUnitDto
{
    public int HospitalId { get; set; }
    public string? HospitalName { get; set; }
    public int UnitId { get; set; }
    public string? UnitName { get; set; }
}
