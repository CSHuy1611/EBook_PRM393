using FluentAssertions;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;

namespace MathIBook.UnitTests;

public class ContentValidationServiceTests
{
    private readonly ContentValidationService _service = new();

    [Fact]
    public void ValidateLesson_RequiresGrade8ExampleAndBalancedLatex()
    {
        var lesson = new Lesson
        {
            Title = "Đại số",
            ContentBody = "Lý thuyết $x+1"
        };

        var result = _service.ValidateLesson(lesson, belongsToGrade8: false);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain(
        [
            "LATEX_INVALID",
            "GRADE8_TOPIC_REQUIRED"
        ]);
    }

    [Fact]
    public void ValidateQuiz_RejectsChapterQuestionOutsideChapter()
    {
        var quiz = new Quiz
        {
            QuizType = QuizType.Chapter,
            ChapterId = Guid.NewGuid(),
            Title = "Kiểm tra chương",
            PassScore = 5,
            DurationSeconds = 600
        };
        var question = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = Guid.NewGuid(),
            QuestionText = "$x+1$ bằng?",
            Options = "[\"1\",\"2\",\"3\",\"4\"]",
            CorrectOption = 0
        };

        var result = _service.ValidateQuiz(quiz, [question], []);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.Code == "CHAPTER_QUESTION_SCOPE_INVALID");
    }

    [Fact]
    public void ValidateQuiz_AcceptsFourOptionLessonQuiz()
    {
        var lessonId = Guid.NewGuid();
        var quiz = new Quiz
        {
            QuizType = QuizType.Lesson,
            LessonId = lessonId,
            Title = "Quiz",
            PassScore = 5,
            DurationSeconds = 600
        };
        var question = new Question
        {
            LessonId = lessonId,
            QuestionText = "$x+1$ bằng?",
            Options = "[\"1\",\"2\",\"3\",\"4\"]",
            CorrectOption = 0
        };

        _service.ValidateQuiz(quiz, [question], []).IsValid.Should().BeTrue();
    }
}
