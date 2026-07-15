using System;
using System.Threading.Tasks;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public partial class AuthService : IAuthService
{
    public async Task SendResetPasswordOtpAsync(ForgotPasswordRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (!EmailRegex().IsMatch(email))
        {
            throw new ArgumentException("Email không hợp lệ.");
        }

        // Verify if user exists in database
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            throw new ArgumentException("Email không tồn tại trong hệ thống.");
        }

        // Generate 6-digit OTP
        var random = new Random();
        var otp = random.Next(100000, 999999).ToString();

        // Store OTP in memory cache for 5 minutes
        var cacheKey = $"reset-otp:{email}";
        _memoryCache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Generated password reset OTP for {Email}: {Otp}", email, otp);

        // Send email
        var subject = "Math-IBook: Yêu cầu đặt lại mật khẩu";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #2196F3; margin: 0;'>Math-IBook</h2>
                    <p style='color: #777; margin: 5px 0 0 0;'>Sách Giáo Khoa Toán 8 Tương Tác</p>
                </div>
                <hr style='border: none; border-top: 1px solid #eee;' />
                <div style='padding: 10px 0;'>
                    <p>Xin chào {user.Name},</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản trên ứng dụng <b>Math-IBook</b>. Dưới đây là mã xác thực OTP của bạn:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; color: #2196F3; letter-spacing: 5px; background: #e3f2fd; padding: 10px 20px; border-radius: 5px; border: 1px dashed #2196F3;'>
                            {otp}
                        </span>
                    </div>
                    <p style='color: #ff9800; font-size: 13px;'><i>Mã OTP này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</i></p>
                </div>
                <hr style='border: none; border-top: 1px solid #eee;' />
                <div style='text-align: center; color: #aaa; font-size: 11px; margin-top: 20px;'>
                    <p>Đây là email tự động từ hệ thống Math-IBook. Vui lòng không phản hồi email này.</p>
                </div>
            </div>";

        await _emailService.SendEmailAsync(email, subject, body);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = NormalizeEmail(request.Email);

        ValidatePassword(request.NewPassword, request.ConfirmNewPassword);

        // Verify OTP
        var isDevelopment = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development"
            || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var isVerified = false;
        if (isDevelopment && request.Otp.Trim() == "123456")
        {
            _logger.LogInformation("Development mode: verified reset password OTP via default code '123456' for {Email}", email);
            isVerified = true;
        }
        else
        {
            var cacheKey = $"reset-otp:{email}";
            if (_memoryCache.TryGetValue(cacheKey, out string? correctOtp))
            {
                if (correctOtp == request.Otp.Trim())
                {
                    _memoryCache.Remove(cacheKey);
                    isVerified = true;
                }
            }
        }

        if (!isVerified)
        {
            throw new ArgumentException("Mã OTP không chính xác hoặc đã hết hạn.");
        }

        // Update password in database
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            throw new ArgumentException("Người dùng không tồn tại.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user: {Email}", email);
    }
}
