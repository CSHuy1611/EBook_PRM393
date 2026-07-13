using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MathIBook.Application.Services;

public partial class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = MapToUserInfo(user)
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        if (request.Password.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters long.");
        }

        if (!MyRegex().IsMatch(request.Password))
        {
            throw new ArgumentException("Password must contain at least one uppercase letter and one digit.");
        }

        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Student",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email} with role {Role}", user.Email, user.Role);

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = MapToUserInfo(user)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            User = MapToUserInfo(user)
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var storedToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (storedToken is not null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            _unitOfWork.RefreshTokens.Update(storedToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateAndStoreRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                     + "-" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return refreshToken;
    }

    private static UserInfo MapToUserInfo(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Coins = user.Coins
        };
    }

    [GeneratedRegex(@"^(?=.*[A-Z])(?=.*\d).+$")]
    private static partial Regex MyRegex();
}
