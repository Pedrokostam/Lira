using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lira.Exceptions;

public class BaseHttpException(HttpStatusCode statusCode, string? message = null, Exception? inner = null) : HttpRequestException(message, inner, statusCode)
{
    public string? ResponseError { get; }

    public static async Task<string> GetErrorResponse(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
