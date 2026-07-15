using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathIBook.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            return Ok(await _authService.LoginAsync(request));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Đăng nhập thất bại.",
                Detail = exception.Message,
                Status = 401
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            return Ok(await _authService.RegisterAsync(request));
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Đăng ký thất bại.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            return Ok(await _authService.RefreshTokenAsync(request.RefreshToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Làm mới phiên đăng nhập thất bại.",
                Detail = exception.Message,
                Status = 401
            });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }
}
