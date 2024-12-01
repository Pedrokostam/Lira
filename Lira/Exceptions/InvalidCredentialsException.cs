using System;
using System.Net;
namespace Lira.Exceptions;

public class InvalidCredentialsException : BaseHttpException
{

    public InvalidCredentialsException(string message) : base(HttpStatusCode.Unauthorized, message)
    {
    }

    public InvalidCredentialsException(string message, Exception? innerException = null) : base(HttpStatusCode.Unauthorized, message, innerException)
    {
    }
}