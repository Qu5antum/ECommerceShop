using System.Net;

namespace App.Exceptions;


public class AlreadyExistsException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public AlreadyExistsException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.Conflict
    ) : base(message)
    {
        StatusCode = statusCode;
    }
}