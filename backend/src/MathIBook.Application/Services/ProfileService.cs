using System.Text.RegularExpressions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;

namespace MathIBook.Application.Services;

public class ProfileService : IProfileService
{
    // Dùng cùng UnitOfWork để đọc nhiều bảng và lưu thay đổi trong một request scope.
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StudentProfileDto> GetAsync(Guid userId)
    {
        // Bảo đảm tài khoản tồn tại, đúng role Student và đang hoạt động.
        var user = await GetActiveStudentAsync(userId);
        // Attempts dùng tính điểm tốt nhất và thành tích quiz.
        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(
                attempt => attempt.UserId == userId))
            .ToList();
        // Chỉ đếm bài/chương đã Passed cho thống kê hoàn thành.
        var progresses = await _unitOfWork.Progresses.FindAsync(
            progress => progress.UserId == userId && progress.Status == LearningStatus.Passed);
        var chapterProgresses = await _unitOfWork.ChapterProgresses.FindAsync(
            progress => progress.UserId == userId && progress.Status == LearningStatus.Passed);
        // Lấy UserBadges để vừa tính badge của user hiện tại vừa tie-break ranking.
        var userBadges = (await _unitOfWork.UserBadges.GetAllAsync()).ToList();
        var badgeCount = userBadges.Count(item => item.UserId == userId);

        // Dùng đúng thứ tự sort của LeaderboardController để rank trên Profile nhất quán.
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
        // FindIndex trả -1 nếu user không nằm trong danh sách; DTO khi đó trả rank=null.
        var rankIndex = students.FindIndex(item => item.User.Id == userId);

        // Điểm trung bình chỉ dùng best score của mỗi quiz chương, không tính mọi lần retry.
        var chapterQuizzes = (await _unitOfWork.Quizzes.FindAsync(
                quiz => quiz.QuizType == QuizType.Chapter && quiz.IsPublished && !quiz.IsDeleted))
            .ToList();
        var chapterQuizIds = chapterQuizzes.Select(q => q.Id).ToHashSet();
        var chapterAttempts = attempts.Where(a => a.QuizId.HasValue && chapterQuizIds.Contains(a.QuizId.Value)).ToList();
        // GroupBy QuizId rồi lấy Score10 cao nhất của từng quiz chương.
        var bestScores = chapterAttempts
            .GroupBy(a => a.QuizId!.Value)
            .Select(g => (double)g.Max(a => a.Score10))
            .ToList();
        var averageScore = bestScores.Count > 0
            ? Math.Round(bestScores.Average(), 2)
            : 0;

        // DTO gom thông tin tài khoản và thành tích cho một lần gọi từ ProfileScreen.
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
            AverageScore = averageScore,
            BestScore = attempts.Count > 0
                ? Math.Round(attempts.Max(attempt => (double)attempt.Score10), 2)
                : 0
        };
    }

    public async Task<StudentProfileDto> UpdateAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await GetActiveStudentAsync(userId);
        // Trim tránh tên chỉ chứa khoảng trắng và lưu dữ liệu nhất quán.
        var name = dto.Name.Trim();
        if (name.Length is < 2 or > 100)
        {
            throw new InvalidOperationException("Họ tên phải có từ 2 đến 100 ký tự.");
        }

        // Giới hạn URL để tránh dữ liệu bất thường vượt kích thước mong muốn.
        if (dto.AvatarUrl?.Length > 500)
        {
            throw new InvalidOperationException("Đường dẫn ảnh đại diện quá dài.");
        }

        // Chuỗi avatar rỗng được quy về null để frontend dùng avatar fallback.
        user.Name = name;
        user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        // Trả GetAsync thay vì DTO tối giản để UI nhận lại cả rank/thống kê mới nhất.
        return await GetAsync(userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await GetActiveStudentAsync(userId);
        // BCrypt.Verify so mật khẩu thuần với hash; không giải mã hash.
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        // Xác nhận ở server dù frontend đã có hai ô nhập, vì client không đáng tin cậy.
        if (dto.NewPassword != dto.ConfirmNewPassword)
        {
            throw new InvalidOperationException("Xác nhận mật khẩu mới không khớp.");
        }

        // Regex yêu cầu ít nhất một chữ hoa và một chữ số.
        if (dto.NewPassword.Length < 6
            || !Regex.IsMatch(dto.NewPassword, @"^(?=.*[A-Z])(?=.*\d).+$"))
        {
            throw new InvalidOperationException(
                "Mật khẩu mới phải có ít nhất 6 ký tự, một chữ hoa và một chữ số.");
        }

        // Không cho tái sử dụng đúng mật khẩu hiện tại.
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        // Hash mới có salt riêng do BCrypt sinh tự động.
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        // Thu hồi mọi phiên đăng nhập khác; access token tự hết hạn theo JWT lifetime.
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
