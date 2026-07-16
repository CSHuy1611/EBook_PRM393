class ChapterModel {
  final String id;
  final String title;
  final String description;
  final int orderIndex;
  final double completionPercentage;
  final int lessonCount;
  final int passedLessonCount;
  final String? chapterQuizId;
  final String chapterQuizStatus;
  final bool isUnlocked;
  final bool isPublished;
  final String? relatedBadgeId;
  final String? relatedBadgeTitle;
  final String? curriculumTopicId;

  ChapterModel({
    required this.id,
    required this.title,
    this.description = '',
    this.orderIndex = 0,
    this.completionPercentage = 0.0,
    this.lessonCount = 0,
    this.passedLessonCount = 0,
    this.chapterQuizId,
    this.chapterQuizStatus = 'Unavailable',
    this.isUnlocked = true,
    this.isPublished = false,
    this.relatedBadgeId,
    this.relatedBadgeTitle,
    this.curriculumTopicId,
  });

  bool get isQuizUnlocked => chapterQuizStatus == 'Unlocked' && isUnlocked;
  bool get isQuizPassed => chapterQuizStatus == 'Passed' && isUnlocked;
  bool get isQuizLocked => chapterQuizStatus == 'Locked' || !isUnlocked;

  factory ChapterModel.fromJson(Map<String, dynamic> json) => ChapterModel(
        id: json['id'] ?? '',
        title: json['title'] ?? '',
        description: json['description'] ?? '',
        orderIndex: json['orderIndex'] ?? json['order_index'] ?? 0,
        completionPercentage: (json['completionPercentage'] ??
                json['completion_percentage'] ??
                0.0)
            .toDouble(),
        lessonCount: json['lessonCount'] ?? json['lesson_count'] ?? 0,
        passedLessonCount: json['passedLessonCount'] ?? json['passed_lesson_count'] ?? 0,
        chapterQuizId: json['chapterQuizId'] ?? json['chapter_quiz_id'],
        chapterQuizStatus: json['chapterQuizStatus'] ?? json['chapter_quiz_status'] ?? 'Unavailable',
        isUnlocked: json['isUnlocked'] ?? json['is_unlocked'] ?? true,
        isPublished: json['isPublished'] ?? json['is_published'] ?? false,
        relatedBadgeId: json['relatedBadgeId'] ?? json['related_badge_id'],
        relatedBadgeTitle: json['relatedBadgeTitle'] ?? json['related_badge_title'],
        curriculumTopicId: json['curriculumTopicId'] ?? json['curriculum_topic_id'],
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'description': description,
        'orderIndex': orderIndex,
        'completionPercentage': completionPercentage,
        'lessonCount': lessonCount,
        'passedLessonCount': passedLessonCount,
        'chapterQuizId': chapterQuizId,
        'chapterQuizStatus': chapterQuizStatus,
        'isUnlocked': isUnlocked,
        'relatedBadgeId': relatedBadgeId,
        'relatedBadgeTitle': relatedBadgeTitle,
        'curriculumTopicId': curriculumTopicId,
      };
}
