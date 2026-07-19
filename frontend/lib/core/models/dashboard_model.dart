import 'quiz_models.dart';

class DashboardDto {
  final double overallCompletionPercentage;
  final int totalCoins;
  final double averageScore;
  final List<ChapterProgressDto> chapterProgress;
  final List<BadgeEarnedDto> badges;
  final List<RecentActivityDto> recentActivities;
  final ContinueLearningDto? continueLearning;
  final int completedLessons;
  final int totalLessons;

  DashboardDto({
    this.overallCompletionPercentage = 0.0,
    this.totalCoins = 0,
    this.averageScore = 0.0,
    this.chapterProgress = const [],
    this.badges = const [],
    this.recentActivities = const [],
    this.continueLearning,
    this.completedLessons = 0,
    this.totalLessons = 0,
  });

  factory DashboardDto.fromJson(Map<String, dynamic> json) {
    final chapterProgressList = <ChapterProgressDto>[];
    if (json['chapterProgress'] != null && json['chapterProgress'] is List) {
      for (final cp in json['chapterProgress']) {
        chapterProgressList.add(ChapterProgressDto.fromJson(cp));
      }
    }
    final badgesList = <BadgeEarnedDto>[];
    if (json['badges'] != null && json['badges'] is List) {
      for (final b in json['badges']) {
        badgesList.add(BadgeEarnedDto.fromJson(b));
      }
    }
    final recentActivitiesList = <RecentActivityDto>[];
    if (json['recentActivities'] != null && json['recentActivities'] is List) {
      for (final ra in json['recentActivities']) {
        recentActivitiesList.add(RecentActivityDto.fromJson(ra));
      }
    }
    return DashboardDto(
      overallCompletionPercentage:
          (json['overallCompletionPercentage'] ?? 0.0).toDouble(),
      totalCoins: json['totalCoins'] ?? 0,
      averageScore: (json['averageScore'] ?? 0.0).toDouble(),
      chapterProgress: chapterProgressList,
      badges: badgesList,
      recentActivities: recentActivitiesList,
      continueLearning: json['continueLearning'] != null
          ? ContinueLearningDto.fromJson(json['continueLearning'])
          : null,
      completedLessons: json['completedLessons'] ?? 0,
      totalLessons: json['totalLessons'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
        'overallCompletionPercentage': overallCompletionPercentage,
        'totalCoins': totalCoins,
        'averageScore': averageScore,
        'chapterProgress':
            chapterProgress.map((cp) => cp.toJson()).toList(),
        'badges': badges.map((b) => b.toJson()).toList(),
        'recentActivities':
            recentActivities.map((ra) => ra.toJson()).toList(),
        'continueLearning': continueLearning?.toJson(),
        'completedLessons': completedLessons,
        'totalLessons': totalLessons,
      };
}

class ContinueLearningDto {
  final String chapterId;
  final String lessonId;
  final String chapterTitle;
  final String lessonTitle;
  final String status;

  ContinueLearningDto({
    required this.chapterId,
    required this.lessonId,
    required this.chapterTitle,
    required this.lessonTitle,
    required this.status,
  });

  factory ContinueLearningDto.fromJson(Map<String, dynamic> json) =>
      ContinueLearningDto(
        chapterId: json['chapterId'] ?? '',
        lessonId: json['lessonId'] ?? '',
        chapterTitle: json['chapterTitle'] ?? '',
        lessonTitle: json['lessonTitle'] ?? '',
        status: json['status'] ?? 'NotStarted',
      );

  Map<String, dynamic> toJson() => {
        'chapterId': chapterId,
        'lessonId': lessonId,
        'chapterTitle': chapterTitle,
        'lessonTitle': lessonTitle,
        'status': status,
      };
}

class ChapterProgressDto {
  final String chapterId;
  final String chapterTitle;
  final int completedLessons;
  final int totalLessons;
  final double completionPercentage;
  final bool isUnlocked;

  ChapterProgressDto({
    required this.chapterId,
    required this.chapterTitle,
    this.completedLessons = 0,
    this.totalLessons = 0,
    this.completionPercentage = 0.0,
    this.isUnlocked = false,
  });

  factory ChapterProgressDto.fromJson(Map<String, dynamic> json) =>
      ChapterProgressDto(
        chapterId: json['chapterId'] ?? '',
        chapterTitle: json['chapterTitle'] ?? '',
        completedLessons: json['completedLessons'] ?? 0,
        totalLessons: json['totalLessons'] ?? 0,
        completionPercentage:
            (json['completionPercentage'] ?? 0.0).toDouble(),
        isUnlocked: json['isUnlocked'] ?? false,
      );

  Map<String, dynamic> toJson() => {
        'chapterId': chapterId,
        'chapterTitle': chapterTitle,
        'completedLessons': completedLessons,
        'totalLessons': totalLessons,
        'completionPercentage': completionPercentage,
        'isUnlocked': isUnlocked,
      };
}

class RecentActivityDto {
  final String type;
  final String description;
  final String timestamp;

  RecentActivityDto({
    required this.type,
    required this.description,
    required this.timestamp,
  });

  factory RecentActivityDto.fromJson(Map<String, dynamic> json) =>
      RecentActivityDto(
        type: json['type'] ?? '',
        description: json['description'] ?? '',
        timestamp: json['timestamp'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'type': type,
        'description': description,
        'timestamp': timestamp,
      };
}
