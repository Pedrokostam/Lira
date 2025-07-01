using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lira.DataTransferObjects;

namespace Lira.Converters;

public class DtoConverterSimplex<TObject, TDto> : JsonConverter<TObject> where TDto : IToObject<TObject>
{
    public override TObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
  
        var dto = JsonSerializer.Deserialize<TDto>(ref reader, options);
        if (dto is null)
        {
            return default;
        }
        return dto.ToObject();
    }

    public override void Write(Utf8JsonWriter writer, TObject value, JsonSerializerOptions options)
    {
        var otherOptions = new JsonSerializerOptions(options);
        var remainingConverters = otherOptions.Converters.Where(x => x is not DtoConverterSimplex<TObject, TDto>).ToList();
        otherOptions.Converters.Clear();
        foreach (var conv in remainingConverters)
        {
            otherOptions.Converters.Add(conv);
        }
        JsonSerializer.Serialize(writer, value, otherOptions);
    }
}
