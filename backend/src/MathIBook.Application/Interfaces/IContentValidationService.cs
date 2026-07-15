using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;

namespace MathIBook.Application.Interfaces;

public interface IContentValidationService
{
    ContentValidationResultDto ValidateLesson(Lesson lesson, bool belongsToGrade8);
    ContentValidationResultDto ValidateQuiz(
        Quiz quiz,
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<Guid> chapterLessonIds);
}
