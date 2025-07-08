using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lira.Converters;
// This converter reads and writes DateTime values according to the "R" standard format specifier:
// https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings#the-rfc1123-r-r-format-specifier.
public partial class JiraDatetimeConverter : JsonConverter<DateTimeOffset>
{
    private const string FormatString = "yyyy-MM-ddTHH:mm:ss.fffzzz";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? datetimeString = reader.GetString();
        if (datetimeString is null)
        {
            return DateTimeOffset.MinValue;
        }

        // Parse the string using DateTimeOffset.ParseExact with the specified format
        return DateTimeOffset.ParseExact(datetimeString, FormatString, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // $DateStarted.ToString("o") -replace "\.(\d{3})\d*([\+\-]\d{2}):", ".`$1`$2"
        // Serialize back to the ISO 8601 string
        // Jira really does not like the colon in timezone, giving error 500
        var datestring = value.ToString(FormatString, CultureInfo.InvariantCulture);
        datestring = DateTimeCorrecter().Replace(datestring, "${tzh}${tzm}");
        writer.WriteStringValue(datestring);
    }
    [GeneratedRegex(@"(?<tzh>\d{2}):(?<tzm>\d{2})$", RegexOptions.ExplicitCapture, 250)]
    private static partial Regex DateTimeCorrecter();
}
