class ChapterModel {
  final String id;
  final String title;
  final String description;
  final int orderIndex;
  final double completionPercentage;
  final int lessonCount;

  ChapterModel({
    required this.id,
    required this.title,
    this.description = '',
    this.orderIndex = 0,
    this.completionPercentage = 0.0,
    this.lessonCount = 0,
  });

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
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'description': description,
        'orderIndex': orderIndex,
        'completionPercentage': completionPercentage,
        'lessonCount': lessonCount,
      };
}
