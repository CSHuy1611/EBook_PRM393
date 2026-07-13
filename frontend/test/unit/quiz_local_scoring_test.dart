import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/models/quiz_models.dart';

/// Calculates the number of correct answers by comparing
/// selected options against correct options.
int calculateScore(List<AnswerDto> answers, List<CorrectAnswerDto> correctAnswers) {
  int score = 0;
  for (final answer in answers) {
    final match = correctAnswers.where((ca) => ca.questionId == answer.questionId);
    if (match.isNotEmpty && match.first.correctOption == answer.selectedOption) {
      score++;
    }
  }
  return score;
}

void main() {
  group('Quiz local scoring', () {
    test('all answers correct returns full score', () {
      final answers = [
        AnswerDto(questionId: 'q1', selectedOption: 1),
        AnswerDto(questionId: 'q2', selectedOption: 2),
        AnswerDto(questionId: 'q3', selectedOption: 0),
      ];
      final correct = [
        CorrectAnswerDto(questionId: 'q1', selectedOption: 1, correctOption: 1, isCorrect: true),
        CorrectAnswerDto(questionId: 'q2', selectedOption: 2, correctOption: 2, isCorrect: true),
        CorrectAnswerDto(questionId: 'q3', selectedOption: 0, correctOption: 0, isCorrect: true),
      ];

      expect(calculateScore(answers, correct), equals(3));
    });

    test('partial correct answers returns partial score', () {
      final answers = [
        AnswerDto(questionId: 'q1', selectedOption: 1),
        AnswerDto(questionId: 'q2', selectedOption: 0),
        AnswerDto(questionId: 'q3', selectedOption: 2),
      ];
      final correct = [
        CorrectAnswerDto(questionId: 'q1', selectedOption: 1, correctOption: 1, isCorrect: true),
        CorrectAnswerDto(questionId: 'q2', selectedOption: 0, correctOption: 2, isCorrect: false),
        CorrectAnswerDto(questionId: 'q3', selectedOption: 2, correctOption: 2, isCorrect: true),
      ];

      expect(calculateScore(answers, correct), equals(2));
    });

    test('no correct answers returns zero', () {
      final answers = [
        AnswerDto(questionId: 'q1', selectedOption: 0),
        AnswerDto(questionId: 'q2', selectedOption: 1),
      ];
      final correct = [
        CorrectAnswerDto(questionId: 'q1', selectedOption: 0, correctOption: 2, isCorrect: false),
        CorrectAnswerDto(questionId: 'q2', selectedOption: 1, correctOption: 3, isCorrect: false),
      ];

      expect(calculateScore(answers, correct), equals(0));
    });

    test('empty answer list returns zero', () {
      final correct = [
        CorrectAnswerDto(questionId: 'q1', selectedOption: 1, correctOption: 1, isCorrect: true),
      ];

      expect(calculateScore([], correct), equals(0));
    });

    test('unmatched question IDs are ignored and not counted as correct', () {
      final answers = [
        AnswerDto(questionId: 'unknown', selectedOption: 1),
      ];
      final correct = [
        CorrectAnswerDto(questionId: 'q1', selectedOption: 1, correctOption: 1, isCorrect: true),
      ];

      expect(calculateScore(answers, correct), equals(0));
    });

    test('score is correct with more correct answers than attempted', () {
      final answers = [
        AnswerDto(questionId: 'q1', selectedOption: 1),
      ];
      final correct = [
        CorrectAnswerDto(questionId: 'q1', selectedOption: 1, correctOption: 1, isCorrect: true),
        CorrectAnswerDto(questionId: 'q2', selectedOption: 0, correctOption: 2, isCorrect: false),
        CorrectAnswerDto(questionId: 'q3', selectedOption: 0, correctOption: 0, isCorrect: true),
      ];

      expect(calculateScore(answers, correct), equals(1));
    });
  });
}
