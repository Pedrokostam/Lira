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
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? datetimeString = reader.GetString();
        if (datetimeString is null)
        {
            return DateTimeOffset.MinValue;
        }

        // Define the exact format of the datetime string
        string format = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        // Parse the string using DateTimeOffset.ParseExact with the specified format
        return DateTimeOffset.ParseExact(datetimeString, format, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // $DateStarted.ToString("o") -replace "\.(\d{3})\d*([\+\-]\d{2}):", ".`$1`$2"
        // Serialize back to the ISO 8601 string
        // Jira really does not like the colon in timezone, giving error 500
        var datestring = value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        datestring = DateTimeCorrecter().Replace(datestring, "$1$2");
        writer.WriteStringValue(datestring);
    }
    private const int _timeout = 250;
#if NETSTANDARD2_0
    private static readonly Regex _dateTimeCorrecter = new Regex(@"(\d{2}):(\d{2})$", RegexOptions.ExplicitCapture|RegexOptions.Compiled, TimeSpan.FromMilliseconds(_timeout));
    private static Regex DateTimeCorrecter() => _dateTimeCorrecter;
#else
    [GeneratedRegex(@"(\d{2}):(\d{2})$", RegexOptions.ExplicitCapture, _timeout)]
    private static partial Regex DateTimeCorrecter();
#endif
}
