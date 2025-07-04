using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Lira.Exceptions;

namespace Lira.Authorization;
public readonly record struct AtlassianApiKey : IAuthorization
{
    public AtlassianApiKey(string userEmail, string apiKey)
    {
        UserEmail = userEmail;
        ApiKey = apiKey;
    }

    public string UserEmail { get; }
    public string ApiKey { get; }
    public string TypeIdentifier => Type;
    public static readonly string Type = "AtlassianApiKey";
    private string GetStringForm()
    {
        return $"{UserEmail}:{ApiKey}";
    }
    public Task Authorize(LiraClient lira)
    {
        var bytes = Encoding.UTF8.GetBytes(GetStringForm());
        var base64 = Convert.ToBase64String(bytes);
        lira.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        return Task.CompletedTask;
    }

    public async Task<Exception?> CreateExceptionForUnauthorized(HttpResponseMessage message)
    {
        var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new BaseHttpException(message.StatusCode, content);
    }

    public Task<bool> EnsureAuthorized(LiraClient lira)
    {
        return Task.FromResult(true);
    }
}
