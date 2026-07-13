using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IBadgeCheckService
{
    Task<List<BadgeEarnedDto>> CheckAndAwardBadgesAsync(Guid userId, Guid? lessonId = null);
}
