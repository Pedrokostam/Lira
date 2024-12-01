using System;
using System.Text.Json;
using System.Text.Json.Serialization;
#if NETSTANDARD2_0
using TimeZoneConverter;
#endif
namespace Lira.Converters;

public class StringToTimeZoneConverter : JsonConverter<TimeZoneInfo?>
{
    private static TimeZoneInfo? FromIanaString(string timeZoneString)
    {
#if NETSTANDARD2_0
        try
        {
            return TimeZoneConverter.TZConvert.GetTimeZoneInfo(timeZoneString);

        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
#else
        //if(TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneString, out var initialWindowsTimezone))
        //{
        //    return initialWindowsTimezone;
        //}
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneString, out var windowsId))
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out var secondWindowsTimezone))
            {
                return secondWindowsTimezone;
            }
            return null;
        }
        return null;
#endif
    }
    private static string? ToIanaString(TimeZoneInfo? timeZoneInfo)
    {
        if (timeZoneInfo is null)
        {
            return null;
        }
#if NETSTANDARD2_0
        try
        {
            return TimeZoneConverter.TZConvert.WindowsToIana(timeZoneInfo.Id);

        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
#else
        //if(TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneString, out var initialWindowsTimezone))
        //{
        //    return initialWindowsTimezone;
        //}
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneInfo.Id, out var ianaId))
        {
            return ianaId;
        }
        return null;
#endif
    }
    public override TimeZoneInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {

        var tzId = reader.GetString();
        if (tzId is null)
        {
            return null;
        }
        return FromIanaString(tzId);
    }

    public override void Write(Utf8JsonWriter writer, TimeZoneInfo? value, JsonSerializerOptions options)
    {
        // Serialize back to the ISO 8601 string
        writer.WriteStringValue(ToIanaString(value));
    }
}