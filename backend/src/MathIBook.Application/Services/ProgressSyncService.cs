using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class ProgressSyncService : IProgressSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProgressSyncService> _logger;

    public ProgressSyncService(IUnitOfWork unitOfWork, ILogger<ProgressSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<ProgressResultDto>> SyncProgressAsync(Guid userId, ProgressSyncDto dto)
    {
        var results = new List<ProgressResultDto>();

        foreach (var item in dto.Items)
        {
            var serverProgress = await _unitOfWork.Progresses.FirstOrDefaultAsync(
                p => p.UserId == userId && p.LessonId == item.LessonId);

            if (serverProgress is null)
            {
                var newProgress = new Progress
                {
                    UserId = userId,
                    LessonId = item.LessonId,
                    IsCompleted = item.IsCompleted,
                    BestScore = item.BestScore,
                    CompletedAt = item.IsCompleted ? DateTime.UtcNow : null,
                    UpdatedAt = DateTime.UtcNow,
                    ClientUpdatedAt = item.ClientUpdatedAt
                };

                await _unitOfWork.Progresses.AddAsync(newProgress);

                results.Add(new ProgressResultDto
                {
                    LessonId = item.LessonId,
                    IsCompleted = item.IsCompleted,
                    BestScore = item.BestScore,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                var useClient = item.ClientUpdatedAt > serverProgress.ClientUpdatedAt
                                || item.BestScore > serverProgress.BestScore;

                if (useClient)
                {
                    serverProgress.IsCompleted = item.IsCompleted;
                    serverProgress.BestScore = item.BestScore;
                    serverProgress.UpdatedAt = DateTime.UtcNow;
                    serverProgress.ClientUpdatedAt = item.ClientUpdatedAt;

                    if (item.IsCompleted && serverProgress.CompletedAt is null)
                    {
                        serverProgress.CompletedAt = DateTime.UtcNow;
                    }

                    _unitOfWork.Progresses.Update(serverProgress);

                    _logger.LogInformation(
                        "Progress synced for user {UserId}, lesson {LessonId}: client data accepted (best_score={BestScore})",
                        userId, item.LessonId, serverProgress.BestScore);
                }

                results.Add(new ProgressResultDto
                {
                    LessonId = serverProgress.LessonId,
                    IsCompleted = serverProgress.IsCompleted,
                    BestScore = serverProgress.BestScore,
                    UpdatedAt = serverProgress.UpdatedAt
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return results;
    }
}
