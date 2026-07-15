using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathIBook.UnitTests;

public class QuizScoringServiceTests
{
    [Fact]
    public async Task ScoreQuizAsync_PersistsVerifiedPassAndUsesConfiguredRewardService()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var user = new User { Id = userId, Role = "Student", IsActive = true };
        var lesson = new Lesson { Id = lessonId, ChapterId = chapterId, IsPublished = true };
        var quiz = new Quiz
        {
            Id = quizId,
            QuizType = QuizType.Lesson,
            LessonId = lessonId,
            PassScore = 5,
            IsPublished = true
        };
        var questions = Enumerable.Range(0, 10)
            .Select(index => new Question
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                CorrectOption = 0,
                Options = "[\"A\",\"B\",\"C\",\"D\"]",
                OrderIndex = index
            })
            .ToList();
        var submission = new QuizSubmitDto
        {
            LessonId = lessonId,
            ClientAttemptId = Guid.NewGuid(),
            ClientCreatedAt = DateTime.UtcNow,
            Answers = questions.Select((question, index) => new AnswerDto
            {
                QuestionId = question.Id,
                SelectedOption = index < 5 ? 0 : 1
            }).ToList()
        };

        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(userId)).ReturnsAsync(user);
        var lessons = new Mock<IRepository<Lesson>>();
        lessons.Setup(repository => repository.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
        lessons.Setup(repository => repository.FindAsync(It.IsAny<Expression<Func<Lesson, bool>>>()))
            .ReturnsAsync([lesson]);

        var quizzes = new Mock<IRepository<Quiz>>();
        quizzes.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync((Expression<Func<Quiz, bool>> predicate) =>
                new[] { quiz }.FirstOrDefault(predicate.Compile()));

        var quizQuestions = new Mock<IRepository<QuizQuestion>>();
        quizQuestions.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<QuizQuestion, bool>>>()))
            .ReturnsAsync([]);
        var questionRepository = new Mock<IRepository<Question>>();
        questionRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Question, bool>>>()))
            .ReturnsAsync((Expression<Func<Question, bool>> predicate) =>
                questions.Where(predicate.Compile()).ToList());

        QuizAttempt? savedAttempt = null;
        var attempts = new Mock<IRepository<QuizAttempt>>();
        attempts.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<QuizAttempt, bool>>>()))
            .ReturnsAsync((QuizAttempt?)null);
        attempts.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<QuizAttempt, bool>>>()))
            .ReturnsAsync([]);
        attempts.Setup(repository => repository.AddAsync(It.IsAny<QuizAttempt>()))
            .Callback<QuizAttempt>(attempt => savedAttempt = attempt)
            .Returns(Task.CompletedTask);

        var attemptAnswers = new Mock<IRepository<QuizAttemptAnswer>>();
        attemptAnswers.Setup(repository => repository.AddAsync(It.IsAny<QuizAttemptAnswer>()))
            .Returns(Task.CompletedTask);

        Progress? savedProgress = null;
        var progresses = new Mock<IRepository<Progress>>();
        progresses.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Progress, bool>>>()))
            .ReturnsAsync((Progress?)null);
        progresses.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Progress, bool>>>()))
            .ReturnsAsync(() => savedProgress is null ? [] : [savedProgress]);
        progresses.Setup(repository => repository.AddAsync(It.IsAny<Progress>()))
            .Callback<Progress>(progress => savedProgress = progress)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.Lessons).Returns(lessons.Object);
        unitOfWork.SetupGet(unit => unit.Quizzes).Returns(quizzes.Object);
        unitOfWork.SetupGet(unit => unit.QuizQuestions).Returns(quizQuestions.Object);
        unitOfWork.SetupGet(unit => unit.Questions).Returns(questionRepository.Object);
        unitOfWork.SetupGet(unit => unit.QuizAttempts).Returns(attempts.Object);
        unitOfWork.SetupGet(unit => unit.QuizAttemptAnswers).Returns(attemptAnswers.Object);
        unitOfWork.SetupGet(unit => unit.Progresses).Returns(progresses.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var rewards = new Mock<IQuizRewardService>();
        rewards.Setup(service => service.AwardAsync(
                userId,
                It.IsAny<QuizRewardContext>()))
            .ReturnsAsync(55);
        var badges = new Mock<IBadgeCheckService>();
        badges.Setup(service => service.CheckAndAwardBadgesAsync(userId, chapterId))
            .ReturnsAsync([]);

        var service = new QuizScoringService(
            unitOfWork.Object,
            Mock.Of<ILogger<QuizScoringService>>(),
            rewards.Object,
            badges.Object);

        var result = await service.ScoreQuizAsync(userId, submission);

        result.Score.Should().Be(5);
        result.IsPassed.Should().BeTrue();
        result.CorrectCount.Should().Be(5);
        result.AttemptNumber.Should().Be(1);
        savedAttempt.Should().NotBeNull();
        savedAttempt!.QuizId.Should().Be(quizId);
        savedAttempt.ClientAttemptId.Should().Be(submission.ClientAttemptId);
        savedAttempt.Score10.Should().Be(5);
        savedAttempt.CoinsEarned.Should().Be(55);
        savedProgress!.Status.Should().Be(LearningStatus.Passed);
        attemptAnswers.Verify(
            repository => repository.AddAsync(It.IsAny<QuizAttemptAnswer>()),
            Times.Exactly(10));
    }

    [Fact]
    public async Task ScoreQuizAsync_RejectsChapterQuizWhenPublishedLessonIsMissing()
    {
        var userId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            QuizType = QuizType.Chapter,
            ChapterId = chapterId,
            PassScore = 5,
            IsPublished = true
        };
        var question = new Question
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            CorrectOption = 0,
            Options = "[\"A\",\"B\",\"C\",\"D\"]"
        };
        var unitOfWork = CreateMinimalUnitOfWork(
            userId,
            quiz,
            [question],
            [new Lesson
            {
                Id = lessonId,
                ChapterId = chapterId,
                Title = "Bài chưa đạt",
                IsPublished = true
            }]);

        var service = new QuizScoringService(
            unitOfWork.Object,
            Mock.Of<ILogger<QuizScoringService>>(),
            Mock.Of<IQuizRewardService>(),
            Mock.Of<IBadgeCheckService>());

        var action = () => service.ScoreQuizAsync(userId, new QuizSubmitDto
        {
            QuizId = quiz.Id,
            Answers = [new AnswerDto { QuestionId = question.Id, SelectedOption = 0 }]
        });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bài chưa đạt*");
    }

    private static Mock<IUnitOfWork> CreateMinimalUnitOfWork(
        Guid userId,
        Quiz quiz,
        List<Question> questions,
        List<Lesson> lessons)
    {
        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Role = "Student", IsActive = true });
        var quizzes = new Mock<IRepository<Quiz>>();
        quizzes.Setup(repository => repository.GetByIdAsync(quiz.Id)).ReturnsAsync(quiz);
        var links = new Mock<IRepository<QuizQuestion>>();
        links.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<QuizQuestion, bool>>>()))
            .ReturnsAsync([]);
        var questionRepository = new Mock<IRepository<Question>>();
        questionRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Question, bool>>>()))
            .ReturnsAsync((Expression<Func<Question, bool>> predicate) =>
                questions.Where(predicate.Compile()).ToList());
        var lessonRepository = new Mock<IRepository<Lesson>>();
        lessonRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Lesson, bool>>>()))
            .ReturnsAsync((Expression<Func<Lesson, bool>> predicate) =>
                lessons.Where(predicate.Compile()).ToList());
        var progresses = new Mock<IRepository<Progress>>();
        progresses.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Progress, bool>>>()))
            .ReturnsAsync([]);
        var attempts = new Mock<IRepository<QuizAttempt>>();
        attempts.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<QuizAttempt, bool>>>()))
            .ReturnsAsync((QuizAttempt?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.Quizzes).Returns(quizzes.Object);
        unitOfWork.SetupGet(unit => unit.QuizQuestions).Returns(links.Object);
        unitOfWork.SetupGet(unit => unit.Questions).Returns(questionRepository.Object);
        unitOfWork.SetupGet(unit => unit.Lessons).Returns(lessonRepository.Object);
        unitOfWork.SetupGet(unit => unit.Progresses).Returns(progresses.Object);
        unitOfWork.SetupGet(unit => unit.QuizAttempts).Returns(attempts.Object);
        return unitOfWork;
    }
}
