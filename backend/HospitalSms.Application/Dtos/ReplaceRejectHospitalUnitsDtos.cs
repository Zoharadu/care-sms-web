namespace HospitalSms.Application.Dtos;

public sealed class ReplaceRejectHospitalUnitsRequest
{
    public int TemplateId { get; set; }
    public List<RejectHospitalUnitItemRequest> Items { get; set; } = [];
}

public sealed class RejectHospitalUnitItemRequest
{
    public int HospitalId { get; set; }
    public int UnitId { get; set; }
}

public sealed class ReplaceRejectHospitalUnitsResponse
{
    public int TemplateId { get; set; }
    public int SavedCount { get; set; }
    public List<RejectHospitalUnitItemResponse> Items { get; set; } = [];
}

public sealed class RejectHospitalUnitItemResponse
{
    public int HospitalId { get; set; }
    public int UnitId { get; set; }
}
