using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;

namespace HospitalSms.Infrastructure.Data.Converters;

internal sealed class TimeRangeValueConverter : ValueConverter<string?, TimeSpan?>
{
    public TimeRangeValueConverter()
        : base(
            value => value == null
                ? null
                : TimeSpan.ParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture),
            value => value.HasValue
                ? value.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
                : null)
    {
    }
}
