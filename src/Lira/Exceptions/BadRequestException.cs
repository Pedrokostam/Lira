using System.Net;

namespace Lira.Exceptions;

public class BadRequestException : BaseHttpException
{
    public BadRequestException(string message) : base(HttpStatusCode.BadRequest, message)
    {

    }
}
