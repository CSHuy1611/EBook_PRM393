using System.Security.Claims;
using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/users")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminUsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive)
    {
        var query = _unitOfWork.Users.Query().Where(user => user.Role == "Student");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(user =>
                user.Name.ToLower().Contains(term)
                || user.Email.ToLower().Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        var users = await query
            .Include(user => user.QuizAttempts)
            .Include(user => user.UserBadges)
            .Include(user => user.Progresses)
            .Include(user => user.ChapterProgresses)
            .OrderBy(user => user.Name)
            .ToListAsync();
        var ranks = await BuildRanksAsync();

        return Ok(users.Select(user => Map(user, ranks)).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDto>> GetById(Guid id)
    {
        var user = await _unitOfWork.Users.Query()
            .Include(item => item.QuizAttempts)
            .Include(item => item.UserBadges)
            .Include(item => item.Progresses)
            .Include(item => item.ChapterProgresses)
            .FirstOrDefaultAsync(item => item.Id == id && item.Role == "Student");
        if (user is null)
        {
            return NotFound();
        }

        return Ok(Map(user, await BuildRanksAsync()));
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<UserHistoryDto>> GetHistory(Guid id)
    {
        var exists = await _unitOfWork.Users.Query()
            .AnyAsync(user => user.Id == id && user.Role == "Student");
        if (!exists)
        {
            return NotFound();
        }

        var attempts = await _unitOfWork.QuizAttempts.Query()
            .Where(attempt => attempt.UserId == id)
            .OrderByDescending(attempt => attempt.CreatedAt)
            .Include(attempt => attempt.Lesson)
            .ThenInclude(lesson => lesson!.Chapter)
            .Include(attempt => attempt.Quiz)
            .ThenInclude(quiz => quiz!.Chapter)
            .ToListAsync();
        var badges = await _unitOfWork.UserBadges.Query()
            .Where(item => item.UserId == id)
            .Include(item => item.Badge)
            .OrderByDescending(item => item.EarnedAt)
            .ToListAsync();
        var transactions = await _unitOfWork.CoinTransactions.Query()
            .Where(item => item.UserId == id)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        var lessonProgress = await _unitOfWork.Progresses.Query()
            .Where(item => item.UserId == id)
            .Include(item => item.Lesson)
            .OrderByDescending(item => item.UpdatedAt)
            .ToListAsync();
        var chapterProgress = await _unitOfWork.ChapterProgresses.Query()
            .Where(item => item.UserId == id)
            .Include(item => item.Chapter)
            .OrderByDescending(item => item.UpdatedAt)
            .ToListAsync();

        return Ok(new UserHistoryDto
        {
            QuizAttempts = attempts.Select(attempt => new QuizAttemptHistoryDto
            {
                Id = attempt.Id,
                QuizId = attempt.QuizId,
                LessonTitle = attempt.Lesson?.Title ?? attempt.Quiz?.Title ?? "Quiz chương",
                ChapterTitle = attempt.Lesson?.Chapter.Title
                    ?? attempt.Quiz?.Chapter?.Title
                    ?? string.Empty,
                Score = (double)attempt.Score10,
                IsPassed = attempt.IsPassed,
                TotalQuestions = attempt.TotalQuestions,
                CoinsEarned = attempt.CoinsEarned,
                DurationSeconds = attempt.DurationSeconds,
                CreatedAt = attempt.CreatedAt
            }).ToList(),
            Badges = badges.Select(item => new BadgeEarnedDto
            {
                BadgeId = item.BadgeId,
                Title = item.Badge.Title,
                Description = item.Badge.Description,
                IconUrl = item.Badge.IconUrl
            }).ToList(),
            CoinTransactions = transactions.Select(item => new CoinTransactionDto
            {
                Amount = item.Amount,
                SourceType = item.SourceType,
                SourceId = item.SourceId,
                BalanceAfter = item.BalanceAfter,
                Description = item.Description,
                CreatedAt = item.CreatedAt
            }).ToList(),
            LessonProgress = lessonProgress.Select(item => new ProgressHistoryDto
            {
                TargetId = item.LessonId,
                Title = item.Lesson.Title,
                Status = item.Status.ToString(),
                BestScore = (double)item.BestScore10,
                UpdatedAt = item.UpdatedAt
            }).ToList(),
            ChapterProgress = chapterProgress.Select(item => new ProgressHistoryDto
            {
                TargetId = item.ChapterId,
                Title = item.Chapter.Title,
                Status = item.Status.ToString(),
                BestScore = (double)item.BestScore10,
                UpdatedAt = item.UpdatedAt
            }).ToList()
        });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] AccountStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Lý do thay đổi trạng thái là bắt buộc.",
                Status = 400
            });
        }

        var student = await _unitOfWork.Users.GetByIdAsync(id);
        if (student is null || student.Role != "Student")
        {
            return NotFound();
        }

        if (student.IsActive == dto.IsActive)
        {
            return NoContent();
        }

        var previousStatus = student.IsActive;
        student.IsActive = dto.IsActive;
        student.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(student);

        if (!dto.IsActive)
        {
            var tokens = await _unitOfWork.RefreshTokens.Query()
                .Where(token => token.UserId == id && token.RevokedAt == null)
                .ToListAsync();
            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                _unitOfWork.RefreshTokens.Update(token);
            }
        }

        await _unitOfWork.ContentAuditLogs.AddAsync(new ContentAuditLog
        {
            AdminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            EntityType = "StudentAccount",
            EntityId = id,
            Action = dto.IsActive ? "Activate" : "Lock",
            BeforeData = JsonSerializer.Serialize(new { isActive = previousStatus }),
            AfterData = JsonSerializer.Serialize(new
            {
                isActive = dto.IsActive,
                reason = dto.Reason.Trim()
            }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Dictionary<Guid, int>> BuildRanksAsync()
    {
        var students = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student" && user.IsActive)
            .Select(user => new
            {
                user.Id,
                user.Coins,
                user.Name,
                BadgeCount = user.UserBadges.Count
            })
            .ToListAsync();

        return students
            .OrderByDescending(item => item.Coins)
            .ThenByDescending(item => item.BadgeCount)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select((item, index) => new { item.Id, Rank = index + 1 })
            .ToDictionary(item => item.Id, item => item.Rank);
    }

    private static AdminUserDto Map(User user, IReadOnlyDictionary<Guid, int> ranks)
    {
        ranks.TryGetValue(user.Id, out var rank);
        return new AdminUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            Coins = user.Coins,
            BadgeCount = user.UserBadges.Count,
            Rank = rank == 0 ? null : rank,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalQuizAttempts = user.QuizAttempts.Count,
            AverageScore = user.QuizAttempts.Count > 0
                ? Math.Round(user.QuizAttempts.Average(attempt => (double)attempt.Score10), 2)
                : 0,
            CompletedLessons = user.Progresses.Count(progress => progress.IsCompleted),
            CompletedChapters = user.ChapterProgresses.Count(progress =>
                progress.Status == Domain.Enums.LearningStatus.Passed)
        };
    }
}
