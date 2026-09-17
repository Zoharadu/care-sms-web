namespace HospitalSms.Application.Dtos;

public sealed class SmsUnitCategoryResponseDto
{
    public int UcId { get; set; }
    public int SmsUnitId { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
}

public sealed class UpdateSmsUnitCategoryStatusRequest
{
    public bool? IsActive { get; set; }
}

public sealed class CreateSmsUnitCategoryRequest
{
    public int ProjectId { get; set; }
    public int CategoryId { get; set; }
    public int HospitalId { get; set; }
    public int SmsUnitId { get; set; }
}

public sealed class SmsUnitCategoryWriteResponseDto
{
    public int UcId { get; set; }
    public int SmsUnitId { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SmsUnitCategoryDetailsDto
{
    public int UcId { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? HospitalId { get; set; }
    public string? HospitalName { get; set; }
    public int SmsUnitId { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public bool IsActive { get; set; }
}
