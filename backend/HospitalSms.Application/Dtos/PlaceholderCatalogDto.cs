namespace HospitalSms.Application.Dtos;

public sealed class PlaceholderCatalogDto
{
    public int PlaceholderId { get; set; }
    public string PlaceholderName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}
