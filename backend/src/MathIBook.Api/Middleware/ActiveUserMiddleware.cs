using System.Security.Claims;
using System.Text.Json;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MathIBook.Api.Middleware;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var userId))
            {
                await RejectAsync(context, "Token không chứa định danh người dùng hợp lệ.");
                return;
            }

            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user is null || !user.IsActive)
            {
                await RejectAsync(context, "Tài khoản không tồn tại hoặc đã bị khóa.");
                return;
            }
        }

        await _next(context);
    }

    private static async Task RejectAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails
        {
            Title = "Phiên đăng nhập không còn hiệu lực.",
            Detail = detail,
            Status = StatusCodes.Status401Unauthorized
        }));
    }
}
