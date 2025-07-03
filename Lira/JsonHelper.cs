using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Lira.Authorization;
using Lira.Converters;
using Lira.DataTransferObjects;
using Lira.Objects;

namespace Lira;


public static class JsonHelper
{
    private static readonly JiraDatetimeConverter _jiraDatetimeConverter = new();
    private static readonly SecondsToTimespanConverter _secondsToTimespanConverter = new();
    private static readonly DtoConverterSimplex<IssueLite, IssueDto> _dtoConverterSimplex_IssueLite = new();
    private static readonly StringToTimeZoneConverter _stringToTimeZoneConverter = new();
    private static readonly AuthorizationConverter _authorizationConverter = new();

    public static JsonSerializerOptions Options => new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {
            _jiraDatetimeConverter,
            _secondsToTimespanConverter,
            _dtoConverterSimplex_IssueLite,
            _stringToTimeZoneConverter,
            _authorizationConverter,
        },
    };

    public static void RegisterAuthorizationType<T>(string key) where T : IAuthorization
    {
        AuthorizationConverter.RegisterType<T>(key);
    }
    public static void RegisterAuthorizationType<T>(T instance) where T : IAuthorization
    {
        AuthorizationConverter.RegisterType(instance);
    }

    public static T? Deserialize<T>(JsonElement jsonElement, string? propertyName = null)
    {
        if (propertyName is null)
        {
            return JsonSerializer.Deserialize<T>(jsonElement, Options);
        }
        jsonElement = jsonElement.GetProperty(propertyName);
        return JsonSerializer.Deserialize<T>(jsonElement, Options);

    }

    public static T? Deserialize<T>(string jsonString, string? propertyName = null)
    {
        if (propertyName is null)
        {
            return JsonSerializer.Deserialize<T>(jsonString, Options);
        }
        using var doc = JsonDocument.Parse(jsonString);
        var elem = doc.RootElement.GetProperty(propertyName);
        return JsonSerializer.Deserialize<T>(elem, Options);
    }

    public static object? Deserialize(string jsonString, Type type, string? propertyName = null)
    {
        if (propertyName is null)
        {
            return JsonSerializer.Deserialize(jsonString, type, Options);
        }
        using var doc = JsonDocument.Parse(jsonString);
        var elem = doc.RootElement.GetProperty(propertyName);
        return JsonSerializer.Deserialize(elem, type, Options);
    }

    public static string Serialize<T>(T item)
    {
        return JsonSerializer.Serialize<T>(item, Options);
    }

    public static async Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync<T>(stream, item, Options, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<T?> DeserializeAsync<T>(Stream jsonStream, string? propertyName = null, CancellationToken cancellationToken = default)
    {
        if (propertyName is null)
        {
            return await JsonSerializer.DeserializeAsync<T>(jsonStream, Options, cancellationToken).ConfigureAwait(false);
        }
        using var doc = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var elem = doc.RootElement.GetProperty(propertyName);
        return JsonSerializer.Deserialize<T>(elem, Options);
    }
    public static void SerializeToFile<T>(T item, string path)
    {
        var test = JsonHelper.Serialize(item);
        File.WriteAllText(path, test);
    }

    internal static T Deserialize<T>(string responseString, object propertyName)
    {
        throw new NotImplementedException();
    }
}

