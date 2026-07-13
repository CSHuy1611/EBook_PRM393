namespace MathIBook.Application.Interfaces;

public interface ICoinCalculationService
{
    Task<int> CalculateQuizCoinsAsync(Guid userId, int score, int totalQuestions, Guid? quizAttemptId);
}
