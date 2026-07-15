using System.Text.RegularExpressions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;

namespace MathIBook.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StudentProfileDto> GetAsync(Guid userId)
    {
        var user = await GetActiveStudentAsync(userId);
        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(
                attempt => attempt.UserId == userId))
            .ToList();
        var progresses = await _unitOfWork.Progresses.FindAsync(
            progress => progress.UserId == userId && progress.Status == LearningStatus.Passed);
        var chapterProgresses = await _unitOfWork.ChapterProgresses.FindAsync(
            progress => progress.UserId == userId && progress.Status == LearningStatus.Passed);
        var userBadges = (await _unitOfWork.UserBadges.GetAllAsync()).ToList();
        var badgeCount = userBadges.Count(item => item.UserId == userId);

        var students = (await _unitOfWork.Users.FindAsync(
                candidate => candidate.Role == "Student" && candidate.IsActive))
            .Select(candidate => new
            {
                User = candidate,
                BadgeCount = userBadges.Count(item => item.UserId == candidate.Id)
            })
            .OrderByDescending(item => item.User.Coins)
            .ThenByDescending(item => item.BadgeCount)
            .ThenBy(item => item.User.Name)
            .ThenBy(item => item.User.Id)
            .ToList();
        var rankIndex = students.FindIndex(item => item.User.Id == userId);

        return new StudentProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Coins = user.Coins,
            BadgeCount = badgeCount,
            Rank = rankIndex >= 0 ? rankIndex + 1 : null,
            CompletedLessons = progresses.Count(),
            CompletedChapters = chapterProgresses.Count(),
            AverageScore = attempts.Count > 0
                ? Math.Round(attempts.Average(attempt => (double)attempt.Score10), 2)
                : 0,
            BestScore = attempts.Count > 0
                ? Math.Round(attempts.Max(attempt => (double)attempt.Score10), 2)
                : 0
        };
    }

    public async Task<StudentProfileDto> UpdateAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await GetActiveStudentAsync(userId);
        var name = dto.Name.Trim();
        if (name.Length is < 2 or > 100)
        {
            throw new InvalidOperationException("Họ tên phải có từ 2 đến 100 ký tự.");
        }

        if (dto.AvatarUrl?.Length > 500)
        {
            throw new InvalidOperationException("Đường dẫn ảnh đại diện quá dài.");
        }

        user.Name = name;
        user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return await GetAsync(userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await GetActiveStudentAsync(userId);
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        if (dto.NewPassword != dto.ConfirmNewPassword)
        {
            throw new InvalidOperationException("Xác nhận mật khẩu mới không khớp.");
        }

        if (dto.NewPassword.Length < 6
            || !Regex.IsMatch(dto.NewPassword, @"^(?=.*[A-Z])(?=.*\d).+$"))
        {
            throw new InvalidOperationException(
                "Mật khẩu mới phải có ít nhất 6 ký tự, một chữ hoa và một chữ số.");
        }

        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        var activeTokens = await _unitOfWork.RefreshTokens.FindAsync(
            token => token.UserId == userId && token.RevokedAt == null);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            _unitOfWork.RefreshTokens.Update(token);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Domain.Entities.User> GetActiveStudentAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null || !user.IsActive || user.Role != "Student")
        {
            throw new UnauthorizedAccessException("Tài khoản học sinh không tồn tại hoặc đã bị khóa.");
        }

        return user;
    }
}
