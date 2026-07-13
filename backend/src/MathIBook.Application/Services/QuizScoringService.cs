using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class QuizScoringService : IQuizScoringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuizScoringService> _logger;
    private readonly ICoinCalculationService _coinCalculationService;
    private readonly IBadgeCheckService _badgeCheckService;

    public QuizScoringService(
        IUnitOfWork unitOfWork,
        ILogger<QuizScoringService> logger,
        ICoinCalculationService coinCalculationService,
        IBadgeCheckService badgeCheckService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _coinCalculationService = coinCalculationService;
        _badgeCheckService = badgeCheckService;
    }

    public async Task<QuizResultDto> ScoreQuizAsync(Guid userId, QuizSubmitDto dto)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(dto.LessonId);
        if (lesson is null)
        {
            throw new InvalidOperationException("Lesson not found.");
        }

        var questions = (await _unitOfWork.Questions.FindAsync(q => q.LessonId == dto.LessonId)).ToList();
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("No questions found for this lesson.");
        }

        var correctAnswers = new List<CorrectAnswerDto>();
        var score = 0;

        foreach (var question in questions)
        {
            var answer = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var selectedOption = answer?.SelectedOption ?? -1;
            var isCorrect = selectedOption == question.CorrectOption;

            if (isCorrect)
            {
                score++;
            }

            correctAnswers.Add(new CorrectAnswerDto
            {
                QuestionId = question.Id,
                SelectedOption = selectedOption,
                CorrectOption = question.CorrectOption,
                IsCorrect = isCorrect,
                Explanation = question.Explanation
            });
        }

        var totalQuestions = questions.Count;
        var scaledScore = totalQuestions > 0
            ? Math.Round((double)score / totalQuestions * 10, 1)
            : 0.0;

        var attempt = new QuizAttempt
        {
            UserId = userId,
            LessonId = dto.LessonId,
            Score = score,
            TotalQuestions = totalQuestions,
            DurationSeconds = dto.DurationSeconds,
            ClientCreatedAt = dto.ClientCreatedAt,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.QuizAttempts.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();

        foreach (var question in questions)
        {
            var answer = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var selectedOption = answer?.SelectedOption ?? -1;

            var attemptAnswer = new QuizAttemptAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = question.Id,
                SelectedOption = selectedOption,
                IsCorrect = selectedOption == question.CorrectOption
            };

            await _unitOfWork.QuizAttemptAnswers.AddAsync(attemptAnswer);
        }

        await _unitOfWork.SaveChangesAsync();

        await UpdateProgressAsync(userId, dto.LessonId, score, totalQuestions);

        var coinsEarned = await _coinCalculationService.CalculateQuizCoinsAsync(userId, score, totalQuestions, attempt.Id);

        var newBadges = await _badgeCheckService.CheckAndAwardBadgesAsync(userId, lesson.ChapterId);

        _logger.LogInformation(
            "User {UserId} scored {Score}/{TotalQuestions} on lesson {LessonId}. Coins earned: {Coins}",
            userId, score, totalQuestions, dto.LessonId, coinsEarned);

        return new QuizResultDto
        {
            Id = attempt.Id,
            Score = scaledScore,
            TotalQuestions = totalQuestions,
            CoinsEarned = coinsEarned,
            CorrectAnswers = correctAnswers,
            NewBadges = newBadges
        };
    }

    private async Task UpdateProgressAsync(Guid userId, Guid lessonId, int score, int totalQuestions)
    {
        var existingProgress = await _unitOfWork.Progresses.FirstOrDefaultAsync(
            p => p.UserId == userId && p.LessonId == lessonId);

        var percentage = totalQuestions > 0 ? (double)score / totalQuestions * 100 : 0;
        var isCompleted = percentage >= 80;

        if (existingProgress is null)
        {
            var progress = new Progress
            {
                UserId = userId,
                LessonId = lessonId,
                IsCompleted = isCompleted,
                BestScore = score,
                CompletedAt = isCompleted ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow,
                ClientUpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Progresses.AddAsync(progress);
        }
        else
        {
            if (score > existingProgress.BestScore)
            {
                existingProgress.BestScore = score;
            }

            if (isCompleted && !existingProgress.IsCompleted)
            {
                existingProgress.IsCompleted = true;
                existingProgress.CompletedAt = DateTime.UtcNow;
            }

            existingProgress.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Progresses.Update(existingProgress);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
