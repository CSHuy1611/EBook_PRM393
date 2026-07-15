using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class ProgressSyncService : IProgressSyncService
{
    private const decimal DefaultPassScore = 5.0m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProgressSyncService> _logger;

    public ProgressSyncService(IUnitOfWork unitOfWork, ILogger<ProgressSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<ProgressResultDto>> SyncProgressAsync(Guid userId, ProgressSyncDto dto)
    {
        if (dto.Items.Select(item => item.LessonId).Distinct().Count() != dto.Items.Count)
        {
            throw new InvalidOperationException("Mỗi bài học chỉ được xuất hiện một lần trong gói đồng bộ.");
        }

        var results = new List<ProgressResultDto>();
        foreach (var item in dto.Items)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(item.LessonId);
            if (lesson is null || lesson.IsDeleted || !lesson.IsPublished)
            {
                throw new InvalidOperationException($"Bài học {item.LessonId} không tồn tại hoặc chưa xuất bản.");
            }

            var progress = await _unitOfWork.Progresses.FirstOrDefaultAsync(
                current => current.UserId == userId && current.LessonId == item.LessonId);
            var now = DateTime.UtcNow;
            if (progress is null)
            {
                progress = new Progress
                {
                    UserId = userId,
                    LessonId = item.LessonId,
                    ClientUpdatedAt = item.ClientUpdatedAt == default ? now : item.ClientUpdatedAt
                };
                progress.MarkContentViewed(now);
                await _unitOfWork.Progresses.AddAsync(progress);
            }
            else if (item.ClientUpdatedAt >= progress.ClientUpdatedAt)
            {
                progress.ClientUpdatedAt = item.ClientUpdatedAt;
                progress.MarkContentViewed(now);
                _unitOfWork.Progresses.Update(progress);
            }

            var attempts = (await _unitOfWork.QuizAttempts.FindAsync(attempt =>
                    attempt.UserId == userId && attempt.LessonId == item.LessonId))
                .ToList();
            if (attempts.Count > 0)
            {
                var bestAttempt = attempts.OrderByDescending(attempt => attempt.Score10).First();
                var quiz = bestAttempt.QuizId.HasValue
                    ? await _unitOfWork.Quizzes.GetByIdAsync(bestAttempt.QuizId.Value)
                    : null;
                progress.BestScore = Math.Max(progress.BestScore, bestAttempt.Score);
                progress.ApplyQuizResult(
                    bestAttempt.Score10,
                    quiz?.PassScore ?? DefaultPassScore,
                    now);
                _unitOfWork.Progresses.Update(progress);
            }

            results.Add(new ProgressResultDto
            {
                LessonId = item.LessonId,
                IsCompleted = progress.IsCompleted,
                BestScore = progress.BestScore,
                UpdatedAt = progress.UpdatedAt
            });

            _logger.LogInformation(
                "Progress synchronized for user {UserId}, lesson {LessonId}; verified attempts: {AttemptCount}",
                userId,
                item.LessonId,
                attempts.Count);
        }

        await _unitOfWork.SaveChangesAsync();
        return results;
    }
}
