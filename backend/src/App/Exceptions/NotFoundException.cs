using System.Net;

namespace App.Exceptions;


public class NotFoundException : Exception
{
    public HttpStatusCode StatusCode { get; }
    
    public NotFoundException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.NotFound
    ) : base(message)
    {
        StatusCode = statusCode;
    }
}