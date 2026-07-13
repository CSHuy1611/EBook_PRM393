class QuizSubmitDto {
  final String lessonId;
  final int durationSeconds;
  final List<AnswerDto> answers;
  final String clientCreatedAt;

  QuizSubmitDto({
    required this.lessonId,
    required this.durationSeconds,
    required this.answers,
    required this.clientCreatedAt,
  });

  factory QuizSubmitDto.fromJson(Map<String, dynamic> json) {
    final answersList = <AnswerDto>[];
    if (json['answers'] != null && json['answers'] is List) {
      for (final a in json['answers']) {
        answersList.add(AnswerDto.fromJson(a));
      }
    }
    return QuizSubmitDto(
      lessonId: json['lessonId'] ?? '',
      durationSeconds: json['durationSeconds'] ?? 0,
      answers: answersList,
      clientCreatedAt: json['clientCreatedAt'] ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
        'lessonId': lessonId,
        'durationSeconds': durationSeconds,
        'answers': answers.map((a) => a.toJson()).toList(),
        'clientCreatedAt': clientCreatedAt,
      };
}

class AnswerDto {
  final String questionId;
  final int selectedOption;

  AnswerDto({
    required this.questionId,
    required this.selectedOption,
  });

  factory AnswerDto.fromJson(Map<String, dynamic> json) => AnswerDto(
        questionId: json['questionId'] ?? '',
        selectedOption: json['selectedOption'] ?? 0,
      );

  Map<String, dynamic> toJson() => {
        'questionId': questionId,
        'selectedOption': selectedOption,
      };
}

class QuizResultDto {
  final String id;
  final double score;
  final int totalQuestions;
  final int coinsEarned;
  final List<CorrectAnswerDto> correctAnswers;
  final List<BadgeEarnedDto> newBadges;

  QuizResultDto({
    this.id = '',
    required this.score,
    required this.totalQuestions,
    this.coinsEarned = 0,
    this.correctAnswers = const [],
    this.newBadges = const [],
  });

  factory QuizResultDto.fromJson(Map<String, dynamic> json) {
    final correctAnswersList = <CorrectAnswerDto>[];
    if (json['correctAnswers'] != null && json['correctAnswers'] is List) {
      for (final ca in json['correctAnswers']) {
        correctAnswersList.add(CorrectAnswerDto.fromJson(ca));
      }
    }
    final badgesList = <BadgeEarnedDto>[];
    if (json['newBadges'] != null && json['newBadges'] is List) {
      for (final b in json['newBadges']) {
        badgesList.add(BadgeEarnedDto.fromJson(b));
      }
    }
    return QuizResultDto(
      id: json['id'] ?? '',
      score: (json['score'] ?? 0).toDouble(),
      totalQuestions: json['totalQuestions'] ?? 0,
      coinsEarned: json['coinsEarned'] ?? 0,
      correctAnswers: correctAnswersList,
      newBadges: badgesList,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'score': score,
        'totalQuestions': totalQuestions,
        'coinsEarned': coinsEarned,
        'correctAnswers': correctAnswers.map((ca) => ca.toJson()).toList(),
        'newBadges': newBadges.map((b) => b.toJson()).toList(),
      };
}

class CorrectAnswerDto {
  final String questionId;
  final int selectedOption;
  final int correctOption;
  final bool isCorrect;
  final String explanation;

  CorrectAnswerDto({
    required this.questionId,
    required this.selectedOption,
    required this.correctOption,
    required this.isCorrect,
    this.explanation = '',
  });

  factory CorrectAnswerDto.fromJson(Map<String, dynamic> json) =>
      CorrectAnswerDto(
        questionId: json['questionId'] ?? '',
        selectedOption: json['selectedOption'] ?? 0,
        correctOption: json['correctOption'] ?? 0,
        isCorrect: json['isCorrect'] ?? false,
        explanation: json['explanation'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'questionId': questionId,
        'selectedOption': selectedOption,
        'correctOption': correctOption,
        'isCorrect': isCorrect,
        'explanation': explanation,
      };
}

class BadgeEarnedDto {
  final String badgeId;
  final String title;
  final String description;
  final String iconUrl;

  BadgeEarnedDto({
    required this.badgeId,
    required this.title,
    this.description = '',
    this.iconUrl = '',
  });

  factory BadgeEarnedDto.fromJson(Map<String, dynamic> json) => BadgeEarnedDto(
        badgeId: json['badgeId'] ?? json['id'] ?? '',
        title: json['title'] ?? '',
        description: json['description'] ?? '',
        iconUrl: json['iconUrl'] ?? json['icon_url'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'badgeId': badgeId,
        'title': title,
        'description': description,
        'iconUrl': iconUrl,
      };
}
