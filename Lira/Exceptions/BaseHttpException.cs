using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lira.Exceptions;

public class BaseHttpException : HttpRequestException
{

#if NETSTANDARD2_0
    public HttpStatusCode StatusCode { get; }
#endif
    public string? ResponseError { get; }
    public BaseHttpException(HttpStatusCode statusCode, string? message = null, Exception? inner = null)
#if NETSTANDARD2_0
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
#else
        : base(message, inner, statusCode)
    {
    }
#endif
    public static async Task<string> GetErrorResponse(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
