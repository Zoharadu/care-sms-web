using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HospitalSms.Application.Dtos;

public static class SmsRuleStopConditions
{
    public const string Discharged = "discharged";
    public const string Never = "never";
    public const string ValidationPattern = "^(?:discharged|never)$";
}

public sealed class CreateSmsRuleRequest
{
    public int TtId { get; set; }
    public int? DependOnTtId { get; set; }
    public double? DependencyMaxMinutes { get; set; } = 0;
    public double? MaxTimeTokefInMinutes { get; set; } = 60;
    public bool? IsConstant { get; set; }

    [StringLength(5, MinimumLength = 5)]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    public string? StartTimeRange { get; set; }

    [StringLength(5, MinimumLength = 5)]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    public string? EndTimeRange { get; set; }

    public bool? IsRecurring { get; set; }

    [Range(1, int.MaxValue)]
    public int? RecurringIntervalDays { get; set; }

    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    public string? RecurringTimeOfDay { get; set; }

    [RegularExpression(SmsRuleStopConditions.ValidationPattern)]
    public string? RecurringStopCondition { get; set; }
}

public sealed class SmsRuleResponse
{
    public int RuleId { get; set; }
    public int TtId { get; set; }
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
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateUser { get; set; }
}

public sealed class UpdateSmsRuleRequest
{
    public int TtId { get; set; }

    private int? _dependOnTtId;
    public int? DependOnTtId
    {
        get => _dependOnTtId;
        set
        {
            _dependOnTtId = value;
            DependOnTtIdSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DependOnTtIdSpecified { get; private set; }

    private double? _dependencyMaxMinutes;
    public double? DependencyMaxMinutes
    {
        get => _dependencyMaxMinutes;
        set
        {
            _dependencyMaxMinutes = value;
            DependencyMaxMinutesSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DependencyMaxMinutesSpecified { get; private set; }

    private double? _maxTimeTokefInMinutes;
    public double? MaxTimeTokefInMinutes
    {
        get => _maxTimeTokefInMinutes;
        set
        {
            _maxTimeTokefInMinutes = value;
            MaxTimeTokefInMinutesSpecified = true;
        }
    }

    [JsonIgnore]
    public bool MaxTimeTokefInMinutesSpecified { get; private set; }

    private bool? _isConstant;
    public bool? IsConstant
    {
        get => _isConstant;
        set
        {
            _isConstant = value;
            IsConstantSpecified = true;
        }
    }

    [JsonIgnore]
    public bool IsConstantSpecified { get; private set; }

    private string? _startTimeRange;

    [StringLength(5, MinimumLength = 5)]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    public string? StartTimeRange
    {
        get => _startTimeRange;
        set
        {
            _startTimeRange = value;
            StartTimeRangeSpecified = true;
        }
    }

    [JsonIgnore]
    public bool StartTimeRangeSpecified { get; private set; }

    private string? _endTimeRange;

    [StringLength(5, MinimumLength = 5)]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    public string? EndTimeRange
    {
        get => _endTimeRange;
        set
        {
            _endTimeRange = value;
            EndTimeRangeSpecified = true;
        }
    }

    [JsonIgnore]
    public bool EndTimeRangeSpecified { get; private set; }

    private bool? _isRecurring;
    public bool? IsRecurring
    {
        get => _isRecurring;
        set
        {
            _isRecurring = value;
            IsRecurringSpecified = true;
        }
    }

    [JsonIgnore]
    public bool IsRecurringSpecified { get; private set; }

    private int? _recurringIntervalDays;

    [Range(1, int.MaxValue)]
    public int? RecurringIntervalDays
    {
        get => _recurringIntervalDays;
        set
        {
            _recurringIntervalDays = value;
            RecurringIntervalDaysSpecified = true;
        }
    }

    [JsonIgnore]
    public bool RecurringIntervalDaysSpecified { get; private set; }

    private string? _recurringTimeOfDay;

    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$")]
    public string? RecurringTimeOfDay
    {
        get => _recurringTimeOfDay;
        set
        {
            _recurringTimeOfDay = value;
            RecurringTimeOfDaySpecified = true;
        }
    }

    [JsonIgnore]
    public bool RecurringTimeOfDaySpecified { get; private set; }

    private string? _recurringStopCondition;

    [RegularExpression(SmsRuleStopConditions.ValidationPattern)]
    public string? RecurringStopCondition
    {
        get => _recurringStopCondition;
        set
        {
            _recurringStopCondition = value;
            RecurringStopConditionSpecified = true;
        }
    }

    [JsonIgnore]
    public bool RecurringStopConditionSpecified { get; private set; }

    [JsonIgnore]
    public bool HasRecurringChanges =>
        IsRecurringSpecified
        || RecurringIntervalDaysSpecified
        || RecurringTimeOfDaySpecified
        || RecurringStopConditionSpecified;
}
