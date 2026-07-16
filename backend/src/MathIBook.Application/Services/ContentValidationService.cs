using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;

namespace MathIBook.Application.Services;

public class ContentValidationService : IContentValidationService
{
    public ContentValidationResultDto ValidateLesson(Lesson lesson, bool belongsToGrade8)
    {
        var result = new ContentValidationResultDto();

        if (string.IsNullOrWhiteSpace(lesson.Title))
        {
            Add(result, "LESSON_TITLE_REQUIRED", "Bài học phải có tiêu đề.", lesson.Id);
        }

        if (string.IsNullOrWhiteSpace(lesson.ContentBody))
        {
            Add(result, "LESSON_CONTENT_REQUIRED", "Bài học phải có nội dung lý thuyết.", lesson.Id);
        }
        else
        {
            var normalized = lesson.ContentBody.ToLowerInvariant();
            if (!normalized.Contains("ví dụ") && !normalized.Contains("example"))
            {
                Add(result, "LESSON_EXAMPLE_REQUIRED", "Bài học phải có ít nhất một phần ví dụ.", lesson.Id);
            }

            if (!HasBalancedLatex(lesson.ContentBody))
            {
                Add(result, "LATEX_INVALID", "Nội dung có cặp ký hiệu LaTeX không cân bằng.", lesson.Id);
            }
        }

        if (!belongsToGrade8)
        {
            Add(result, "GRADE8_TOPIC_REQUIRED", "Bài học phải thuộc taxonomy Toán lớp 8.", lesson.Id);
        }

        return result;
    }

    public ContentValidationResultDto ValidateQuiz(
        Quiz quiz,
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<Guid> chapterLessonIds)
    {
        var result = new ContentValidationResultDto();

        if (!quiz.HasValidTarget())
        {
            Add(result, "QUIZ_TARGET_INVALID", "Quiz phải thuộc đúng một bài học hoặc một chương.", quiz.Id);
        }

        if (string.IsNullOrWhiteSpace(quiz.Title))
        {
            Add(result, "QUIZ_TITLE_REQUIRED", "Quiz phải có tiêu đề.", quiz.Id);
        }

        if (quiz.PassScore is < 0 or > 10)
        {
            Add(result, "PASS_SCORE_INVALID", "Điểm đạt phải nằm trong khoảng 0 đến 10.", quiz.Id);
        }

        if (quiz.DurationSeconds <= 0)
        {
            Add(result, "DURATION_INVALID", "Thời lượng quiz phải lớn hơn 0.", quiz.Id);
        }

        if (questions.Count == 0)
        {
            Add(result, "QUIZ_QUESTION_REQUIRED", "Quiz phải có ít nhất một câu hỏi.", quiz.Id);
        }

        foreach (var question in questions)
        {
            List<string>? options = null;
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(question.Options);
            }
            catch (JsonException)
            {
            }

            if (options is null
                || options.Count != 4
                || options.Any(string.IsNullOrWhiteSpace))
            {
                Add(
                    result,
                    "QUESTION_OPTIONS_INVALID",
                    "Mỗi câu hỏi phải có đúng 4 lựa chọn không rỗng.",
                    question.Id);
            }

            if (question.CorrectOption is < 0 or > 3)
            {
                Add(
                    result,
                    "CORRECT_OPTION_INVALID",
                    "Đáp án đúng phải là một trong 4 lựa chọn.",
                    question.Id);
            }

            if (!HasBalancedLatex(question.QuestionText)
                || options?.Any(option => !HasBalancedLatex(option)) == true)
            {
                Add(
                    result,
                    "QUESTION_LATEX_INVALID",
                    "Câu hỏi hoặc lựa chọn có LaTeX không hợp lệ.",
                    question.Id);
            }

            if (quiz.QuizType == QuizType.Lesson && question.LessonId != quiz.LessonId)
            {
                Add(
                    result,
                    "LESSON_QUESTION_SCOPE_INVALID",
                    "Câu hỏi không thuộc bài học của quiz.",
                    question.Id);
            }

            if (quiz.QuizType == QuizType.Chapter)
            {
                var belongsDirectly = question.ChapterId == quiz.ChapterId;
                var belongsThroughLesson = question.LessonId.HasValue
                    && chapterLessonIds.Contains(question.LessonId.Value);
                if (!belongsDirectly && !belongsThroughLesson)
                {
                    Add(
                        result,
                        "CHAPTER_QUESTION_SCOPE_INVALID",
                        "Câu hỏi của quiz chương phải thuộc chương đó.",
                        question.Id);
                }
            }
        }

        return result;
    }

    internal static bool HasBalancedLatex(string text)
    {
        var dollarCount = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '$' && (index == 0 || text[index - 1] != '\\'))
            {
                dollarCount++;
            }
        }

        return dollarCount % 2 == 0
            && Count(text, @"\(") == Count(text, @"\)")
            && Count(text, @"\[") == Count(text, @"\]");
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void Add(
        ContentValidationResultDto result,
        string code,
        string message,
        Guid entityId)
    {
        result.Errors.Add(new ContentValidationErrorDto
        {
            Code = code,
            Message = message,
            EntityId = entityId
        });
    }
}
