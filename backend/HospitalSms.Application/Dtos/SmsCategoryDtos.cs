namespace HospitalSms.Application.Dtos;

public sealed class SmsCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int ProjectId { get; set; }
    public string? CategoryType { get; set; }
    public bool IsActive { get; set; }
    public bool IsEditable { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}

public sealed class SaveSmsCategoryRequest
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int? ProjectId { get; set; }
    public string? CategoryType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEditable { get; set; } = true;
}
