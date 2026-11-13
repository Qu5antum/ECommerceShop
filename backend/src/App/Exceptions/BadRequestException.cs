using System.Net;

namespace App.Exceptions;


public class BadRequestException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public BadRequestException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest
    ) : base(message)
    {
        StatusCode = statusCode;
    }
}