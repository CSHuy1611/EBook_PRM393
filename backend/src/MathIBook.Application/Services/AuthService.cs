using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using MathIBook.Application.Common;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MathIBook.Application.Services;

public partial class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IEmailService _emailService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IMemoryCache memoryCache,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _memoryCache = memoryCache;
        _emailService = emailService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", email);
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        return BuildResponse(user, accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var name = request.Name.Trim();
        var email = NormalizeEmail(request.Email);

        if (name.Length is < 2 or > 100)
        {
            throw new ArgumentException("Họ tên phải có từ 2 đến 100 ký tự.");
        }

        if (!EmailRegex().IsMatch(email))
        {
            throw new ArgumentException("Email không hợp lệ.");
        }

        ValidatePassword(request.Password, request.ConfirmPassword);

        // Verify OTP
        if (string.IsNullOrEmpty(request.Otp) || !VerifyOtp(email, request.Otp.Trim()))
        {
            throw new ArgumentException("Mã OTP không chính xác hoặc đã hết hạn.");
        }

        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(user => user.Email.ToLower() == email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Student",
            IsActive = true,
            LastLoginAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("New student registered: {Email}", user.Email);

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        return BuildResponse(user, accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var decryptedToken = EncryptionHelper.Decrypt(refreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
            token => token.Token == decryptedToken);

        if (storedToken is null
            || storedToken.RevokedAt is not null
            || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user is null || !user.IsActive)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            _unitOfWork.RefreshTokens.Update(storedToken);
            await _unitOfWork.SaveChangesAsync();
            throw new UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị khóa.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync();

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        return BuildResponse(user, newAccessToken, newRefreshToken.Token);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var decryptedToken = EncryptionHelper.Decrypt(refreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
            token => token.Token == decryptedToken);
        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.SaveChangesAsync();
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(
            _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

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
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateAndStoreRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = $"{Convert.ToBase64String(Guid.NewGuid().ToByteArray())}-{Convert.ToBase64String(Guid.NewGuid().ToByteArray())}",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
        return refreshToken;
    }

    private static AuthResponse BuildResponse(User user, string accessToken, string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = EncryptionHelper.Encrypt(accessToken),
            RefreshToken = EncryptionHelper.Encrypt(refreshToken),
            User = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Coins = user.Coins
            }
        };
    }

    private static void ValidatePassword(string password, string confirmation)
    {
        if (password != confirmation)
        {
            throw new ArgumentException("Xác nhận mật khẩu không khớp.");
        }

        if (password.Length < 6 || !PasswordRegex().IsMatch(password))
        {
            throw new ArgumentException(
                "Mật khẩu phải có ít nhất 6 ký tự, một chữ hoa và một chữ số.");
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    [GeneratedRegex(@"^(?=.*[A-Z])(?=.*\d).+$")]
    private static partial Regex PasswordRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
