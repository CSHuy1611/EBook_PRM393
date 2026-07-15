using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IQuizRewardService
{
    Task<int> AwardAsync(Guid userId, QuizRewardContext context);
}
