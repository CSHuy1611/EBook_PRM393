using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IProgressSyncService
{
    Task<List<ProgressResultDto>> SyncProgressAsync(Guid userId, ProgressSyncDto dto);
}
