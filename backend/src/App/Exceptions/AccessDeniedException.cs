using System.Net;

namespace App.Exceptions;


public class AccessDeniedException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public AccessDeniedException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.Forbidden
    ) : base(message)
    {
        StatusCode = statusCode;
    }
}