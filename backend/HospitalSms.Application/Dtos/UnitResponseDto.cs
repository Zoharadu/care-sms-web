namespace HospitalSms.Application.Dtos;

public sealed class UnitResponseDto
{
    public int SmsUnitId { get; set; }
    public int HospitalId { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = null!;
    public bool IsActive { get; set; }
}
