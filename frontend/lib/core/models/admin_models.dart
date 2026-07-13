import 'quiz_models.dart';

class AdminUserDto {
  final String id;
  final String name;
  final String email;
  final String role;
  final int coins;
  final String createdAt;
  final int totalQuizAttempts;
  final double averageScore;

  AdminUserDto({
    required this.id,
    required this.name,
    required this.email,
    this.role = 'Student',
    this.coins = 0,
    this.createdAt = '',
    this.totalQuizAttempts = 0,
    this.averageScore = 0.0,
  });

  factory AdminUserDto.fromJson(Map<String, dynamic> json) => AdminUserDto(
        id: json['id'] ?? '',
        name: json['name'] ?? '',
        email: json['email'] ?? '',
        role: json['role'] ?? 'Student',
        coins: json['coins'] ?? 0,
        createdAt: json['createdAt'] ?? '',
        totalQuizAttempts: json['totalQuizAttempts'] ?? 0,
        averageScore: (json['averageScore'] ?? 0.0).toDouble(),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'email': email,
        'role': role,
        'coins': coins,
        'createdAt': createdAt,
        'totalQuizAttempts': totalQuizAttempts,
        'averageScore': averageScore,
      };
}

class UserHistoryDto {
  final List<QuizAttemptHistoryDto> quizAttempts;
  final List<BadgeEarnedDto> badges;
  final List<CoinTransactionDto> coinTransactions;

  UserHistoryDto({
    this.quizAttempts = const [],
    this.badges = const [],
    this.coinTransactions = const [],
  });

  factory UserHistoryDto.fromJson(Map<String, dynamic> json) {
    final quizAttemptsList = <QuizAttemptHistoryDto>[];
    if (json['quizAttempts'] != null && json['quizAttempts'] is List) {
      for (final qa in json['quizAttempts']) {
        quizAttemptsList.add(QuizAttemptHistoryDto.fromJson(qa));
      }
    }
    final badgesList = <BadgeEarnedDto>[];
    if (json['badges'] != null && json['badges'] is List) {
      for (final b in json['badges']) {
        badgesList.add(BadgeEarnedDto.fromJson(b));
      }
    }
    final coinTransactionsList = <CoinTransactionDto>[];
    if (json['coinTransactions'] != null && json['coinTransactions'] is List) {
      for (final ct in json['coinTransactions']) {
        coinTransactionsList.add(CoinTransactionDto.fromJson(ct));
      }
    }
    return UserHistoryDto(
      quizAttempts: quizAttemptsList,
      badges: badgesList,
      coinTransactions: coinTransactionsList,
    );
  }

  Map<String, dynamic> toJson() => {
        'quizAttempts': quizAttempts.map((qa) => qa.toJson()).toList(),
        'badges': badges.map((b) => b.toJson()).toList(),
        'coinTransactions':
            coinTransactions.map((ct) => ct.toJson()).toList(),
      };
}

class QuizAttemptHistoryDto {
  final String id;
  final String lessonTitle;
  final String chapterTitle;
  final int score;
  final int totalQuestions;
  final int durationSeconds;
  final String createdAt;

  QuizAttemptHistoryDto({
    this.id = '',
    this.lessonTitle = '',
    this.chapterTitle = '',
    this.score = 0,
    this.totalQuestions = 0,
    this.durationSeconds = 0,
    this.createdAt = '',
  });

  factory QuizAttemptHistoryDto.fromJson(Map<String, dynamic> json) =>
      QuizAttemptHistoryDto(
        id: json['id'] ?? '',
        lessonTitle: json['lessonTitle'] ?? '',
        chapterTitle: json['chapterTitle'] ?? '',
        score: json['score'] ?? 0,
        totalQuestions: json['totalQuestions'] ?? 0,
        durationSeconds: json['durationSeconds'] ?? 0,
        createdAt: json['createdAt'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'lessonTitle': lessonTitle,
        'chapterTitle': chapterTitle,
        'score': score,
        'totalQuestions': totalQuestions,
        'durationSeconds': durationSeconds,
        'createdAt': createdAt,
      };
}

class CoinTransactionDto {
  final int amount;
  final String sourceType;
  final String description;
  final String createdAt;

  CoinTransactionDto({
    required this.amount,
    required this.sourceType,
    this.description = '',
    this.createdAt = '',
  });

  factory CoinTransactionDto.fromJson(Map<String, dynamic> json) =>
      CoinTransactionDto(
        amount: json['amount'] ?? 0,
        sourceType: json['sourceType'] ?? '',
        description: json['description'] ?? '',
        createdAt: json['createdAt'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'amount': amount,
        'sourceType': sourceType,
        'description': description,
        'createdAt': createdAt,
      };
}

class ReportOverviewDto {
  final int totalUsers;
  final int totalQuizAttempts;
  final double overallAverageScore;
  final int totalCoinsAwarded;
  final int totalBadgesAwarded;
  final List<ChapterReportDto> chapterReports;
  final List<DailyActivityDto> dailyActivities;

  ReportOverviewDto({
    this.totalUsers = 0,
    this.totalQuizAttempts = 0,
    this.overallAverageScore = 0.0,
    this.totalCoinsAwarded = 0,
    this.totalBadgesAwarded = 0,
    this.chapterReports = const [],
    this.dailyActivities = const [],
  });

  factory ReportOverviewDto.fromJson(Map<String, dynamic> json) {
    final chapterReportsList = <ChapterReportDto>[];
    if (json['chapterReports'] != null && json['chapterReports'] is List) {
      for (final cr in json['chapterReports']) {
        chapterReportsList.add(ChapterReportDto.fromJson(cr));
      }
    }
    final dailyActivitiesList = <DailyActivityDto>[];
    if (json['dailyActivities'] != null && json['dailyActivities'] is List) {
      for (final da in json['dailyActivities']) {
        dailyActivitiesList.add(DailyActivityDto.fromJson(da));
      }
    }
    return ReportOverviewDto(
      totalUsers: json['totalUsers'] ?? 0,
      totalQuizAttempts: json['totalQuizAttempts'] ?? 0,
      overallAverageScore: (json['overallAverageScore'] ?? 0.0).toDouble(),
      totalCoinsAwarded: json['totalCoinsAwarded'] ?? 0,
      totalBadgesAwarded: json['totalBadgesAwarded'] ?? 0,
      chapterReports: chapterReportsList,
      dailyActivities: dailyActivitiesList,
    );
  }

  Map<String, dynamic> toJson() => {
        'totalUsers': totalUsers,
        'totalQuizAttempts': totalQuizAttempts,
        'overallAverageScore': overallAverageScore,
        'totalCoinsAwarded': totalCoinsAwarded,
        'totalBadgesAwarded': totalBadgesAwarded,
        'chapterReports':
            chapterReports.map((cr) => cr.toJson()).toList(),
        'dailyActivities':
            dailyActivities.map((da) => da.toJson()).toList(),
      };
}

class ChapterReportDto {
  final String chapterId;
  final String chapterTitle;
  final int totalAttempts;
  final double averageScore;
  final double completionRate;

  ChapterReportDto({
    required this.chapterId,
    this.chapterTitle = '',
    this.totalAttempts = 0,
    this.averageScore = 0.0,
    this.completionRate = 0.0,
  });

  factory ChapterReportDto.fromJson(Map<String, dynamic> json) =>
      ChapterReportDto(
        chapterId: json['chapterId'] ?? '',
        chapterTitle: json['chapterTitle'] ?? '',
        totalAttempts: json['totalAttempts'] ?? 0,
        averageScore: (json['averageScore'] ?? 0.0).toDouble(),
        completionRate: (json['completionRate'] ?? 0.0).toDouble(),
      );

  Map<String, dynamic> toJson() => {
        'chapterId': chapterId,
        'chapterTitle': chapterTitle,
        'totalAttempts': totalAttempts,
        'averageScore': averageScore,
        'completionRate': completionRate,
      };
}

class DailyActivityDto {
  final String date;
  final int quizCount;
  final int newUsers;

  DailyActivityDto({
    required this.date,
    this.quizCount = 0,
    this.newUsers = 0,
  });

  factory DailyActivityDto.fromJson(Map<String, dynamic> json) =>
      DailyActivityDto(
        date: json['date'] ?? '',
        quizCount: json['quizCount'] ?? 0,
        newUsers: json['newUsers'] ?? 0,
      );

  Map<String, dynamic> toJson() => {
        'date': date,
        'quizCount': quizCount,
        'newUsers': newUsers,
      };
}
