using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lira.DataTransferObjects;

namespace Lira.Converters;

public class DtoConverterDuplex<TObject, TDto> : JsonConverter<TObject> where TDto : IToObject<TObject>
    where TObject : IToDto<TDto>
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
        JsonSerializer.Serialize(writer, value.ToDto(), options);
    }
}
