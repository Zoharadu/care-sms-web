using HospitalSms.Domain.Entities;

namespace HospitalSms.Application.Dtos;

public class TemplateWithBodyDto
{
    public int Id { get; set; }
    public int? UnitId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Bodies { get; set; } = new();
}

public class AssociatedUnitDto
{
    public int HospitalId { get; set; }
    public int UnitId { get; set; }
    public string HospitalName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
}

public class ClinicalRuleAggregate
{
    public SmsTemplate Template { get; set; } = null!;
    public SmsCategory Category { get; set; } = null!;
    public List<SmsTemplateLanguage> Languages { get; set; } = new();
    public List<AssociatedUnitDto> AssociatedUnits { get; set; } = new();
    public SmsCategoryStep Step { get; set; } = null!;
    public SmsTrigger Trigger { get; set; } = null!;
}
