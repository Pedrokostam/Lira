using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lira.Objects;

namespace Lira.Converters;

public class SecondsToTimespanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long datetimeString = reader.GetInt64();
        return TimeSpan.FromSeconds(datetimeString);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Serialize back to the ISO 8601 string
        writer.WriteNumberValue((long)value.TotalSeconds);
    }
}
