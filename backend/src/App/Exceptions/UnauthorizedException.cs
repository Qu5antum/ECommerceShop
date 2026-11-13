using System.Net;

namespace App.Exceptions;


public class UnauthorizedException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public UnauthorizedException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.Unauthorized
    ) : base(message)
    {
        StatusCode = statusCode;
    }
}