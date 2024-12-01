using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Lira.Exceptions;

namespace Lira.Authorization;

public readonly record struct PersonalAccessToken(
    [property: JsonPropertyName("value")] string Value
    ) : IAuthorization
{
    public string Name { get; } = "PAT";

    public Task Authorize(LiraClient lira)
    {
        lira.HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", Value);
        return Task.CompletedTask;
    }
    public void Save(string filepath)
    {
        JsonHelper.SerializeToFile(this, filepath);
    }
    public static PersonalAccessToken Load(string filepath)
    {
        return JsonHelper.Deserialize<PersonalAccessToken>(filepath);
    }

    public Task<bool> EnsureAuthorized(LiraClient lira) => Task.FromResult(true);

    public async Task<Exception?> CreateExceptionForUnauthorized(HttpResponseMessage message)
    {
        var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new BaseHttpException(message.StatusCode, content);
    }
}
