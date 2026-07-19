import 'dart:convert';

class LessonModel {
  final String id;
  final String chapterId;
  final String? curriculumTopicId;
  final String title;
  final String contentBody;
  final String simulationType;
  final int orderIndex;
  final bool isPublished;
  final bool isCompleted;
  final String status;
  final double bestScore;
  final List<QuestionModel> questions;

  LessonModel({
    required this.id,
    required this.chapterId,
    this.curriculumTopicId,
    required this.title,
    this.contentBody = '',
    this.simulationType = '',
    this.orderIndex = 0,
    this.isPublished = true,
    this.isCompleted = false,
    this.status = 'NotStarted',
    this.bestScore = 0.0,
    this.questions = const [],
  });

  factory LessonModel.fromJson(Map<String, dynamic> json) {
    final questionsList = <QuestionModel>[];
    if (json['questions'] != null && json['questions'] is List) {
      for (final q in json['questions']) {
        questionsList.add(QuestionModel.fromJson(q));
      }
    }
    return LessonModel(
      id: json['id'] ?? '',
      chapterId: json['chapterId'] ?? json['chapter_id'] ?? '',
      curriculumTopicId: json['curriculumTopicId'] ?? json['curriculum_topic_id'],
      title: json['title'] ?? '',
      contentBody: json['contentBody'] ?? json['content_body'] ?? '',
      simulationType:
          json['simulationType'] ?? json['simulation_type'] ?? '',
      orderIndex: json['orderIndex'] ?? json['order_index'] ?? 0,
      isPublished: json['isPublished'] ?? json['is_published'] ?? true,
      isCompleted: json['isCompleted'] ?? json['is_completed'] ?? false,
      status: json['status'] ?? 'NotStarted',
      bestScore:
          (json['bestScore'] ?? json['best_score'] ?? 0.0).toDouble(),
      questions: questionsList,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'chapterId': chapterId,
        'curriculumTopicId': curriculumTopicId,
        'title': title,
        'contentBody': contentBody,
        'simulationType': simulationType,
        'orderIndex': orderIndex,
        'isPublished': isPublished,
        'isCompleted': isCompleted,
        'status': status,
        'bestScore': bestScore,
        'questions': questions.map((q) => q.toJson()).toList(),
      };
}

class QuestionModel {
  final String id;
  final String lessonId;
  final String? chapterId;
  final String questionText;
  final List<String> options;
  final int? correctOption;
  final int orderIndex;
  final String explanation;

  QuestionModel({
    required this.id,
    required this.lessonId,
    this.chapterId,
    required this.questionText,
    this.options = const [],
    this.correctOption,
    this.orderIndex = 0,
    this.explanation = '',
  });

  factory QuestionModel.fromJson(Map<String, dynamic> json) {
    List<String> optionsList = [];
    if (json['options'] != null) {
      if (json['options'] is List) {
        optionsList = (json['options'] as List).map((e) => e.toString()).toList();
      } else if (json['options'] is String) {
        try {
          final decoded = jsonDecode(json['options']);
          if (decoded is List) optionsList = decoded.map((e) => e.toString()).toList();
        } catch (_) {}
      }
    }
    return QuestionModel(
      id: json['id'] ?? '',
      lessonId: json['lessonId'] ?? json['lesson_id'] ?? '',
      chapterId: json['chapterId'] ?? json['chapter_id'],
      questionText: json['questionText'] ?? json['question_text'] ?? '',
      options: optionsList,
      correctOption: json['correctOption'] ?? json['correct_option'],
      orderIndex: json['orderIndex'] ?? json['order_index'] ?? 0,
      explanation: json['explanation'] ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'lessonId': lessonId,
        'chapterId': chapterId,
        'questionText': questionText,
        'options': options,
        'correctOption': correctOption,
        'explanation': explanation,
        'orderIndex': orderIndex,
      };
}
