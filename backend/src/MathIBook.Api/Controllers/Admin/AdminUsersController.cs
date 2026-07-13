using MathIBook.Application.DTOs;
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
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var users = await _unitOfWork.Users.Query()
                .OrderBy(u => u.CreatedAt)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    Coins = u.Coins,
                    CreatedAt = u.CreatedAt,
                    TotalQuizAttempts = u.QuizAttempts.Count,
                    AverageScore = u.QuizAttempts.Any()
                        ? Math.Round(u.QuizAttempts.Average(qa => (double)qa.Score / qa.TotalQuestions * 100), 2)
                        : 0
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching users",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
                return NotFound(new ProblemDetails { Title = "User not found", Status = 404 });

            var quizAttempts = await _unitOfWork.QuizAttempts.Query()
                .Where(qa => qa.UserId == id)
                .OrderByDescending(qa => qa.CreatedAt)
                .Include(qa => qa.Lesson)
                .ThenInclude(l => l.Chapter)
                .ToListAsync();

            var badges = await _unitOfWork.UserBadges.Query()
                .Where(ub => ub.UserId == id)
                .Include(ub => ub.Badge)
                .ToListAsync();

            var coinTransactions = await _unitOfWork.CoinTransactions.Query()
                .Where(ct => ct.UserId == id)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            var result = new UserHistoryDto
            {
                QuizAttempts = quizAttempts.Select(qa => new QuizAttemptHistoryDto
                {
                    Id = qa.Id,
                    LessonTitle = qa.Lesson.Title,
                    ChapterTitle = qa.Lesson.Chapter.Title,
                    Score = qa.Score,
                    TotalQuestions = qa.TotalQuestions,
                    DurationSeconds = qa.DurationSeconds,
                    CreatedAt = qa.CreatedAt
                }).ToList(),
                Badges = badges.Select(ub => new BadgeEarnedDto
                {
                    BadgeId = ub.Badge.Id,
                    Title = ub.Badge.Title,
                    Description = ub.Badge.Description,
                    IconUrl = ub.Badge.IconUrl
                }).ToList(),
                CoinTransactions = coinTransactions.Select(ct => new CoinTransactionDto
                {
                    Amount = ct.Amount,
                    SourceType = ct.SourceType,
                    Description = ct.Description,
                    CreatedAt = ct.CreatedAt
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching user history",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
