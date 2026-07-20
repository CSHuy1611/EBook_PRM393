using System.Net;
using System.Text.Json;

namespace MathIBook.Api.Middleware;

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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { statusCode = 409, message = ex.Message });
            await context.Response.WriteAsync(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { statusCode = 403, message = ex.Message });
            await context.Response.WriteAsync(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { statusCode = 404, message = ex.Message });
            await context.Response.WriteAsync(result);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update exception");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var isDev = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;
            var result = JsonSerializer.Serialize(new { statusCode = 409, message = isDev ? ex.InnerException?.Message ?? ex.Message : "Database constraint violation" });
            await context.Response.WriteAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var isDev = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;
            var result = JsonSerializer.Serialize(new { statusCode = 500, message = isDev ? ex.Message : "Internal server error" });
            await context.Response.WriteAsync(result);
        }
    }
}
