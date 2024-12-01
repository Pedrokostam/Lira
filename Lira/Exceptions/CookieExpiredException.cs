using System;
using System.Net;
namespace Lira.Exceptions;
[Serializable]
public class CookieExpiredException : BaseHttpException
{

    public CookieExpiredException(string message) : base(HttpStatusCode.Unauthorized, message)
    {
    }

    public CookieExpiredException(string message, Exception? innerException = null) : base(HttpStatusCode.Unauthorized, message, innerException)
    {
    }
}
