namespace HospitalSms.Application.Dtos;

public sealed class SmsProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public bool IsActive { get; set; }
}
