class AdminQuizModel {
  final String id;
  final int quizType;
  final String? lessonId;
  final String? chapterId;
  final String? rewardPolicyId;
  final String title;
  final double passScore;
  final int durationSeconds;
  final int firstPassCoins;
  final bool isPublished;
  final int questionCount;
  final String? publishedAt;

  AdminQuizModel({
    required this.id,
    required this.quizType,
    this.lessonId,
    this.chapterId,
    this.rewardPolicyId,
    required this.title,
    required this.passScore,
    required this.durationSeconds,
    required this.firstPassCoins,
    required this.isPublished,
    required this.questionCount,
    this.publishedAt,
  });

  factory AdminQuizModel.fromJson(Map<String, dynamic> json) {
    return AdminQuizModel(
      id: json['id'] ?? '',
      quizType: json['quizType'] ?? 0,
      lessonId: json['lessonId'],
      chapterId: json['chapterId'],
      rewardPolicyId: json['rewardPolicyId'],
      title: json['title'] ?? '',
      passScore: (json['passScore'] ?? 5.0).toDouble(),
      durationSeconds: json['durationSeconds'] ?? 0,
      firstPassCoins: json['firstPassCoins'] ?? 0,
      isPublished: json['isPublished'] ?? false,
      questionCount: json['questionCount'] ?? 0,
      publishedAt: json['publishedAt'],
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'quizType': quizType,
        'lessonId': lessonId,
        'chapterId': chapterId,
        'rewardPolicyId': rewardPolicyId,
        'title': title,
        'passScore': passScore,
        'durationSeconds': durationSeconds,
        'firstPassCoins': firstPassCoins,
        'isPublished': isPublished,
        'questionCount': questionCount,
        'publishedAt': publishedAt,
      };
}
