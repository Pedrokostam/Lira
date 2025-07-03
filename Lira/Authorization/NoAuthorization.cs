using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Lira.Exceptions;

namespace Lira.Authorization;
public  class NoAuthorization : IAuthorization
{
    public static readonly NoAuthorization Instance = new ();

    public string TypeIdentifier => Type;
    public static readonly string Type = "NoAuthorization";
    private NoAuthorization()
    {
    }
    public Task Authorize(LiraClient lira)
    {
        return Task.CompletedTask;
    }

    public async Task<Exception?> CreateExceptionForUnauthorized(HttpResponseMessage message)
    {
        var error = await BaseHttpException.GetErrorResponse(message).ConfigureAwait(false);
        return new BaseHttpException(message.StatusCode,error);
    }

    public Task<bool> EnsureAuthorized(LiraClient lira)
    {
        return Task.FromResult(true);
    }
}
