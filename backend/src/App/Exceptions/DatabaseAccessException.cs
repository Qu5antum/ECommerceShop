using System.Net;

namespace App.Exceptions;


public class DatabaseException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public DatabaseException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError
    ) : base(message)
    {
        StatusCode = statusCode;
    }
}