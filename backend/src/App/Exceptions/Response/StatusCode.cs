using System.Net;

namespace App.Exceptions;


public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); 
        }

        catch (NotFoundException ex) 
        {
            _logger.LogWarning(ex, "NotFound exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)ex.StatusCode; 
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = (int)ex.StatusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        catch (AccessDeniedException ex)
        {
            _logger.LogWarning(ex, "Access denied exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)ex.StatusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = (int)ex.StatusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        catch (AlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Already exists exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)ex.StatusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = (int)ex.StatusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)ex.StatusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = (int)ex.StatusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        catch (DatabaseException ex)
        {
            _logger.LogWarning(ex, "Database exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)ex.StatusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = (int)ex.StatusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)ex.StatusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = (int)ex.StatusCode,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }

        catch (Exception ex) 
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                statusCode = 500,
                message = "Internal server error."
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}