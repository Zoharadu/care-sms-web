using Microsoft.AspNetCore.Mvc;

namespace HospitalSms.Admin.Api.Models;

public sealed class SmsCategoryQuery
{
    [FromQuery(Name = "projectId")]
    public int? ProjectId { get; set; }

    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; set; }
}

public sealed class SmsTemplateQuery
{
    [FromQuery(Name = "projectId")]
    public int? ProjectId { get; set; }

    [FromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }

    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; set; }
}

public sealed class PlaceholderQuery
{
    [FromQuery(Name = "projectId")]
    public int? ProjectId { get; set; }

    [FromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }
}

public sealed class SmsRulesQuery
{
    [FromQuery(Name = "projectId")]
    public int? ProjectId { get; set; }

    [FromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }
}
