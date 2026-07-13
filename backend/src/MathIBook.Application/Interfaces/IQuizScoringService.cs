using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IQuizScoringService
{
    Task<QuizResultDto> ScoreQuizAsync(Guid userId, QuizSubmitDto dto);
}
