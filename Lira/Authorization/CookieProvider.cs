using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Lira.Exceptions;
using System.Security.Cryptography;
using Lira.Extensions;
namespace Lira.Authorization;

public record CookieProvider(string Username, string Password) : IAuthorization
{
    public string TypeIdentifier => Type;
    public static readonly string Type = "CookieProvider";

    long _lastCheckTime;
    readonly TimeSpan _checkPeriod = TimeSpan.FromMinutes(60);
    public async Task Authorize(LiraClient lira)
    {
        Dictionary<string, string> credentials = new(StringComparer.Ordinal)
        {
            { "username", Username },
            { "password", Password },
        };
        var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");
        var cookieResponse = await lira.HttpClient.PostAsync(LiraClient.LoginEndpoint, content, lira.CancellationTokenSource.Token).ConfigureAwait(false);

        // The POST request should gives us a few cookies that the HttpClient automatically stored
        // those cookies are used for authentication

        var cookies = cookieResponse.Headers.GetValues("Set-Cookie").ToList();
        if (!cookieResponse.IsSuccessStatusCode || !cookies.Exists(x => x.Contains("SESSIONID", StringComparison.OrdinalIgnoreCase)))
        {
            var error = await BaseHttpException.GetErrorResponse(cookieResponse).ConfigureAwait(false);
            var exc = new BaseHttpException(cookieResponse.StatusCode, error);
            lira.Logger.LogErrorResponse(exc);
            throw exc;
        }
    }

    public async Task<bool> EnsureAuthorized(LiraClient lira)
    {
        var diff = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _lastCheckTime);
        if (diff < _checkPeriod)
        {
            return true;
        }
        lira.Logger.CookieProviderRecheck();
        var response = await lira.HttpClient.GetAsync(LiraClient.LoginEndpoint).ConfigureAwait(false);
        _lastCheckTime = response.IsSuccessStatusCode ? Stopwatch.GetTimestamp() : 0;
        lira.Logger.CookieProviderRecheckFinished();
        return response.IsSuccessStatusCode;
    }


    public async Task<Exception?> CreateExceptionForUnauthorized(HttpResponseMessage message)
    {
        var c = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        var phrase = message.ReasonPhrase?.ToLowerInvariant() ?? "";
        if (phrase.Contains("expire",StringComparison.OrdinalIgnoreCase))
        {
            var errorResponse = await BaseHttpException.GetErrorResponse(message).ConfigureAwait(false);
            return new CookieExpiredException(errorResponse);
            //return Task.FromResult<Exception?>(new CookieExpiredException());
        }
        return new HttpRequestException();
        //return Task.FromResult<Exception?>(new HttpRequestException());
    }
}