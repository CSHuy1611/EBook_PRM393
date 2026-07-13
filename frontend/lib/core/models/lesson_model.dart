class LessonModel {
  final String id;
  final String chapterId;
  final String title;
  final String contentBody;
  final String simulationType;
  final int orderIndex;
  final bool isPublished;
  final bool isCompleted;
  final double bestScore;
  final List<QuestionModel> questions;

  LessonModel({
    required this.id,
    required this.chapterId,
    required this.title,
    this.contentBody = '',
    this.simulationType = '',
    this.orderIndex = 0,
    this.isPublished = true,
    this.isCompleted = false,
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
      title: json['title'] ?? '',
      contentBody: json['contentBody'] ?? json['content_body'] ?? '',
      simulationType:
          json['simulationType'] ?? json['simulation_type'] ?? '',
      orderIndex: json['orderIndex'] ?? json['order_index'] ?? 0,
      isPublished: json['isPublished'] ?? json['is_published'] ?? true,
      isCompleted: json['isCompleted'] ?? json['is_completed'] ?? false,
      bestScore:
          (json['bestScore'] ?? json['best_score'] ?? 0.0).toDouble(),
      questions: questionsList,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'chapterId': chapterId,
        'title': title,
        'contentBody': contentBody,
        'simulationType': simulationType,
        'orderIndex': orderIndex,
        'isPublished': isPublished,
        'isCompleted': isCompleted,
        'bestScore': bestScore,
        'questions': questions.map((q) => q.toJson()).toList(),
      };
}

class QuestionModel {
  final String id;
  final String lessonId;
  final String questionText;
  final List<String> options;
  final int? correctOption;
  final String explanation;
  final int orderIndex;

  QuestionModel({
    required this.id,
    required this.lessonId,
    required this.questionText,
    this.options = const [],
    this.correctOption,
    this.explanation = '',
    this.orderIndex = 0,
  });

  factory QuestionModel.fromJson(Map<String, dynamic> json) {
    List<String> optionsList = [];
    if (json['options'] != null) {
      if (json['options'] is List) {
        optionsList = (json['options'] as List).map((e) => e.toString()).toList();
      } else if (json['options'] is String) {
        optionsList = (json['options'] as String).split(',').map((e) => e.trim()).toList();
      }
    }
    return QuestionModel(
      id: json['id'] ?? '',
      lessonId: json['lessonId'] ?? json['lesson_id'] ?? '',
      questionText: json['questionText'] ?? json['question_text'] ?? '',
      options: optionsList,
      correctOption: json['correctOption'] ?? json['correct_option'],
      explanation: json['explanation'] ?? '',
      orderIndex: json['orderIndex'] ?? json['order_index'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'lessonId': lessonId,
        'questionText': questionText,
        'options': options,
        'correctOption': correctOption,
        'explanation': explanation,
        'orderIndex': orderIndex,
      };
}
