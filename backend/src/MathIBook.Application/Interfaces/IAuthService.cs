using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
