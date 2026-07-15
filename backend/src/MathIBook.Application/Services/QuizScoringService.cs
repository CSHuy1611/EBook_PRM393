using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class QuizScoringService : IQuizScoringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuizScoringService> _logger;
    private readonly IQuizRewardService _rewardService;
    private readonly IBadgeCheckService _badgeCheckService;

    public QuizScoringService(
        IUnitOfWork unitOfWork,
        ILogger<QuizScoringService> logger,
        IQuizRewardService rewardService,
        IBadgeCheckService badgeCheckService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _rewardService = rewardService;
        _badgeCheckService = badgeCheckService;
    }

    public async Task<QuizResultDto> ScoreQuizAsync(Guid userId, QuizSubmitDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null || !user.IsActive || user.Role != "Student")
        {
            throw new UnauthorizedAccessException("Tài khoản học sinh không tồn tại hoặc đã bị khóa.");
        }

        var quiz = await ResolveQuizAsync(dto);
        var questions = await LoadQuestionsAsync(quiz);
        ValidateSubmission(dto, questions);

        var clientAttemptId = dto.ClientAttemptId ?? Guid.NewGuid();
        var existingAttempt = await _unitOfWork.QuizAttempts.FirstOrDefaultAsync(
            attempt => attempt.UserId == userId && attempt.ClientAttemptId == clientAttemptId);
        if (existingAttempt is not null)
        {
            return await BuildExistingResultAsync(existingAttempt, quiz, questions);
        }

        if (quiz.QuizType == QuizType.Chapter)
        {
            await EnsureChapterQuizUnlockedAsync(userId, quiz.ChapterId!.Value);
        }

        var priorAttempts = (await _unitOfWork.QuizAttempts.FindAsync(
                attempt => attempt.UserId == userId && attempt.QuizId == quiz.Id))
            .OrderBy(attempt => attempt.CreatedAt)
            .ToList();

        var answerMap = dto.Answers.ToDictionary(answer => answer.QuestionId);
        var correctAnswers = new List<CorrectAnswerDto>();
        var score = 0;

        foreach (var question in questions)
        {
            var selectedOption = answerMap[question.Id].SelectedOption;
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

        var now = DateTime.UtcNow;
        var score10 = Math.Round((decimal)score / questions.Count * 10m, 2);
        var attempt = new QuizAttempt
        {
            UserId = userId,
            LessonId = quiz.LessonId,
            QuizId = quiz.Id,
            ClientAttemptId = clientAttemptId,
            Score = score,
            TotalQuestions = questions.Count,
            Score10 = score10,
            IsPassed = score10 >= quiz.PassScore,
            DurationSeconds = dto.DurationSeconds,
            ClientCreatedAt = dto.ClientCreatedAt == default ? now : dto.ClientCreatedAt,
            CreatedAt = now,
            SyncedAt = now
        };

        await _unitOfWork.QuizAttempts.AddAsync(attempt);
        foreach (var question in questions)
        {
            var selectedOption = answerMap[question.Id].SelectedOption;
            await _unitOfWork.QuizAttemptAnswers.AddAsync(new QuizAttemptAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = question.Id,
                SelectedOption = selectedOption,
                IsCorrect = selectedOption == question.CorrectOption
            });
        }

        await _unitOfWork.SaveChangesAsync();

        var progressResult = quiz.QuizType == QuizType.Lesson
            ? await UpdateLessonProgressAsync(userId, quiz, score, score10, now)
            : await UpdateChapterProgressAsync(userId, quiz, score10, now);

        var coinsEarned = await _rewardService.AwardAsync(userId, new QuizRewardContext
        {
            Quiz = quiz,
            Attempt = attempt,
            IsFirstPass = progressResult.IsFirstPass,
            IsRetry = priorAttempts.Count > 0
        });

        attempt.CoinsEarned = coinsEarned;
        attempt.RewardProcessedAt = DateTime.UtcNow;
        _unitOfWork.QuizAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync();

        var chapterId = quiz.ChapterId ?? progressResult.ChapterId;
        var newBadges = await _badgeCheckService.CheckAndAwardBadgesAsync(userId, chapterId);

        _logger.LogInformation(
            "User {UserId} scored {Score}/{TotalQuestions} on quiz {QuizId}. Coins earned: {Coins}",
            userId,
            score,
            questions.Count,
            quiz.Id,
            coinsEarned);

        return new QuizResultDto
        {
            Id = attempt.Id,
            QuizId = quiz.Id,
            ClientAttemptId = clientAttemptId,
            Score = (double)score10,
            IsPassed = attempt.IsPassed,
            PassScore = (double)quiz.PassScore,
            CorrectCount = score,
            TotalQuestions = questions.Count,
            AttemptNumber = priorAttempts.Count + 1,
            CoinsEarned = coinsEarned,
            CorrectAnswers = correctAnswers,
            NewBadges = newBadges
        };
    }

    private async Task<Quiz> ResolveQuizAsync(QuizSubmitDto dto)
    {
        Quiz? quiz;
        if (dto.QuizId.HasValue)
        {
            quiz = await _unitOfWork.Quizzes.GetByIdAsync(dto.QuizId.Value);
        }
        else if (dto.LessonId.HasValue)
        {
            quiz = await _unitOfWork.Quizzes.FirstOrDefaultAsync(
                candidate =>
                    candidate.LessonId == dto.LessonId.Value
                    && candidate.QuizType == QuizType.Lesson
                    && !candidate.IsDeleted);
        }
        else
        {
            throw new InvalidOperationException("QuizId hoặc LessonId là bắt buộc.");
        }

        if (quiz is null || quiz.IsDeleted || !quiz.IsPublished || !quiz.HasValidTarget())
        {
            throw new InvalidOperationException("Bài kiểm tra không tồn tại hoặc chưa được xuất bản.");
        }

        if (dto.LessonId.HasValue && quiz.LessonId != dto.LessonId)
        {
            throw new InvalidOperationException("Bài kiểm tra không thuộc bài học đã chọn.");
        }

        return quiz;
    }

    private async Task<List<Question>> LoadQuestionsAsync(Quiz quiz)
    {
        var links = (await _unitOfWork.QuizQuestions.FindAsync(link => link.QuizId == quiz.Id))
            .OrderBy(link => link.OrderIndex)
            .ToList();

        var questions = new List<Question>();
        foreach (var link in links)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(link.QuestionId);
            if (question is not null && !question.IsDeleted)
            {
                questions.Add(question);
            }
        }

        if (questions.Count == 0)
        {
            questions = quiz.QuizType == QuizType.Lesson
                ? (await _unitOfWork.Questions.FindAsync(
                    question => question.LessonId == quiz.LessonId && !question.IsDeleted))
                    .OrderBy(question => question.OrderIndex)
                    .ToList()
                : (await _unitOfWork.Questions.FindAsync(
                    question => question.ChapterId == quiz.ChapterId && !question.IsDeleted))
                    .OrderBy(question => question.OrderIndex)
                    .ToList();
        }

        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Bài kiểm tra chưa có câu hỏi hợp lệ.");
        }

        return questions;
    }

    private static void ValidateSubmission(QuizSubmitDto dto, IReadOnlyCollection<Question> questions)
    {
        if (dto.DurationSeconds < 0)
        {
            throw new InvalidOperationException("Thời gian làm bài không hợp lệ.");
        }

        var duplicates = dto.Answers
            .GroupBy(answer => answer.QuestionId)
            .Any(group => group.Count() > 1);
        if (duplicates)
        {
            throw new InvalidOperationException("Mỗi câu hỏi chỉ được gửi một đáp án.");
        }

        var questionIds = questions.Select(question => question.Id).ToHashSet();
        if (dto.Answers.Count != questions.Count
            || dto.Answers.Any(answer => !questionIds.Contains(answer.QuestionId)))
        {
            throw new InvalidOperationException("Phải trả lời đầy đủ đúng các câu hỏi của bài kiểm tra.");
        }

        if (dto.Answers.Any(answer => answer.SelectedOption is < 0 or > 3))
        {
            throw new InvalidOperationException("Đáp án lựa chọn phải nằm trong khoảng 0 đến 3.");
        }
    }

    private async Task EnsureChapterQuizUnlockedAsync(Guid userId, Guid chapterId)
    {
        var lessons = (await _unitOfWork.Lessons.FindAsync(
                lesson =>
                    lesson.ChapterId == chapterId
                    && lesson.IsPublished
                    && !lesson.IsDeleted))
            .OrderBy(lesson => lesson.OrderIndex)
            .ToList();

        if (lessons.Count == 0)
        {
            throw new InvalidOperationException("Chương chưa có bài học đã xuất bản.");
        }

        var progress = (await _unitOfWork.Progresses.FindAsync(item => item.UserId == userId))
            .ToDictionary(item => item.LessonId);

        var missing = lessons
            .Where(lesson => !progress.TryGetValue(lesson.Id, out var item) || item.Status != LearningStatus.Passed)
            .Select(lesson => lesson.Title)
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Bài kiểm tra chương đang bị khóa. Cần hoàn thành: {string.Join(", ", missing)}.");
        }
    }

    private async Task<ProgressUpdateResult> UpdateLessonProgressAsync(
        Guid userId,
        Quiz quiz,
        int score,
        decimal score10,
        DateTime occurredAt)
    {
        var lessonId = quiz.LessonId!.Value;
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId)
            ?? throw new InvalidOperationException("Bài học không tồn tại.");

        var progress = await _unitOfWork.Progresses.FirstOrDefaultAsync(
            item => item.UserId == userId && item.LessonId == lessonId);
        var wasPassed = progress?.Status == LearningStatus.Passed;

        if (progress is null)
        {
            progress = new Progress
            {
                UserId = userId,
                LessonId = lessonId,
                BestScore = score,
                ClientUpdatedAt = occurredAt
            };
            progress.ApplyQuizResult(score10, quiz.PassScore, occurredAt);
            await _unitOfWork.Progresses.AddAsync(progress);
        }
        else
        {
            progress.BestScore = Math.Max(progress.BestScore, score);
            progress.ClientUpdatedAt = occurredAt;
            progress.ApplyQuizResult(score10, quiz.PassScore, occurredAt);
            _unitOfWork.Progresses.Update(progress);
        }

        await _unitOfWork.SaveChangesAsync();
        if (progress.Status == LearningStatus.Passed)
        {
            await TryUnlockChapterQuizAsync(userId, lesson.ChapterId, occurredAt);
        }

        return new ProgressUpdateResult
        {
            ChapterId = lesson.ChapterId,
            IsFirstPass = !wasPassed && progress.Status == LearningStatus.Passed
        };
    }

    private async Task<ProgressUpdateResult> UpdateChapterProgressAsync(
        Guid userId,
        Quiz quiz,
        decimal score10,
        DateTime occurredAt)
    {
        var chapterId = quiz.ChapterId!.Value;
        var progress = await _unitOfWork.ChapterProgresses.FirstOrDefaultAsync(
            item => item.UserId == userId && item.ChapterId == chapterId);
        var wasPassed = progress?.Status == LearningStatus.Passed;

        if (progress is null)
        {
            progress = new ChapterProgress
            {
                UserId = userId,
                ChapterId = chapterId,
                ClientUpdatedAt = occurredAt
            };
            progress.Unlock(occurredAt);
            progress.ApplyQuizResult(score10, quiz.PassScore, occurredAt);
            await _unitOfWork.ChapterProgresses.AddAsync(progress);
        }
        else
        {
            progress.Unlock(occurredAt);
            progress.ClientUpdatedAt = occurredAt;
            progress.ApplyQuizResult(score10, quiz.PassScore, occurredAt);
            _unitOfWork.ChapterProgresses.Update(progress);
        }

        await _unitOfWork.SaveChangesAsync();
        return new ProgressUpdateResult
        {
            ChapterId = chapterId,
            IsFirstPass = !wasPassed && progress.Status == LearningStatus.Passed
        };
    }

    private async Task TryUnlockChapterQuizAsync(Guid userId, Guid chapterId, DateTime occurredAt)
    {
        var chapterQuiz = await _unitOfWork.Quizzes.FirstOrDefaultAsync(
            quiz =>
                quiz.ChapterId == chapterId
                && quiz.QuizType == QuizType.Chapter
                && quiz.IsPublished
                && !quiz.IsDeleted);
        if (chapterQuiz is null)
        {
            return;
        }

        var lessons = (await _unitOfWork.Lessons.FindAsync(
            lesson => lesson.ChapterId == chapterId && lesson.IsPublished && !lesson.IsDeleted)).ToList();
        var progresses = (await _unitOfWork.Progresses.FindAsync(item => item.UserId == userId)).ToList();
        if (lessons.Count == 0
            || lessons.Any(lesson => !progresses.Any(item =>
                item.LessonId == lesson.Id && item.Status == LearningStatus.Passed)))
        {
            return;
        }

        var chapterProgress = await _unitOfWork.ChapterProgresses.FirstOrDefaultAsync(
            item => item.UserId == userId && item.ChapterId == chapterId);
        if (chapterProgress?.QuizUnlockedAt is not null)
        {
            return;
        }

        if (chapterProgress is null)
        {
            chapterProgress = new ChapterProgress { UserId = userId, ChapterId = chapterId };
            chapterProgress.Unlock(occurredAt);
            await _unitOfWork.ChapterProgresses.AddAsync(chapterProgress);
        }
        else
        {
            chapterProgress.Unlock(occurredAt);
            _unitOfWork.ChapterProgresses.Update(chapterProgress);
        }

        await _unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = userId,
            Title = "Đã mở khóa bài kiểm tra chương",
            Body = $"Bạn đã hoàn thành các bài học và mở khóa “{chapterQuiz.Title}”.",
            Link = $"/chapters/{chapterId}/quiz",
            Type = "chapter_quiz_unlocked",
            RelatedEntityId = chapterQuiz.Id,
            CreatedAt = occurredAt
        });
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<QuizResultDto> BuildExistingResultAsync(
        QuizAttempt attempt,
        Quiz quiz,
        IReadOnlyCollection<Question> questions)
    {
        var answers = (await _unitOfWork.QuizAttemptAnswers.FindAsync(
                answer => answer.AttemptId == attempt.Id))
            .ToDictionary(answer => answer.QuestionId);
        var priorCount = (await _unitOfWork.QuizAttempts.FindAsync(
            item => item.UserId == attempt.UserId
                && item.QuizId == attempt.QuizId
                && item.CreatedAt <= attempt.CreatedAt)).Count();

        return new QuizResultDto
        {
            Id = attempt.Id,
            QuizId = quiz.Id,
            ClientAttemptId = attempt.ClientAttemptId ?? attempt.Id,
            Score = (double)attempt.Score10,
            IsPassed = attempt.IsPassed,
            PassScore = (double)quiz.PassScore,
            CorrectCount = attempt.Score,
            TotalQuestions = attempt.TotalQuestions,
            AttemptNumber = Math.Max(1, priorCount),
            CoinsEarned = attempt.CoinsEarned,
            IsDuplicate = true,
            CorrectAnswers = questions.Select(question =>
            {
                answers.TryGetValue(question.Id, out var answer);
                var selectedOption = answer?.SelectedOption ?? -1;
                return new CorrectAnswerDto
                {
                    QuestionId = question.Id,
                    SelectedOption = selectedOption,
                    CorrectOption = question.CorrectOption,
                    IsCorrect = selectedOption == question.CorrectOption,
                    Explanation = question.Explanation
                };
            }).ToList()
        };
    }

    private sealed class ProgressUpdateResult
    {
        public Guid ChapterId { get; init; }
        public bool IsFirstPass { get; init; }
    }
}
