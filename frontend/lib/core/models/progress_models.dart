class ProgressSyncDto {
  final List<ProgressItemDto> items;

  ProgressSyncDto({required this.items});

  factory ProgressSyncDto.fromJson(Map<String, dynamic> json) {
    final itemsList = <ProgressItemDto>[];
    if (json['items'] != null && json['items'] is List) {
      for (final item in json['items']) {
        itemsList.add(ProgressItemDto.fromJson(item));
      }
    }
    return ProgressSyncDto(items: itemsList);
  }

  Map<String, dynamic> toJson() => {
        'items': items.map((i) => i.toJson()).toList(),
      };
}

class ProgressItemDto {
  final String lessonId;
  final bool isCompleted;
  final double bestScore;
  final String clientUpdatedAt;

  ProgressItemDto({
    required this.lessonId,
    required this.isCompleted,
    required this.bestScore,
    required this.clientUpdatedAt,
  });

  factory ProgressItemDto.fromJson(Map<String, dynamic> json) =>
      ProgressItemDto(
        lessonId: json['lessonId'] ?? '',
        isCompleted: json['isCompleted'] ?? false,
        bestScore: (json['bestScore'] ?? 0.0).toDouble(),
        clientUpdatedAt: json['clientUpdatedAt'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'lessonId': lessonId,
        'isCompleted': isCompleted,
        'bestScore': bestScore,
        'clientUpdatedAt': clientUpdatedAt,
      };
}

class ProgressResultDto {
  final String lessonId;
  final bool isCompleted;
  final double bestScore;
  final String updatedAt;

  ProgressResultDto({
    required this.lessonId,
    required this.isCompleted,
    required this.bestScore,
    required this.updatedAt,
  });

  factory ProgressResultDto.fromJson(Map<String, dynamic> json) =>
      ProgressResultDto(
        lessonId: json['lessonId'] ?? '',
        isCompleted: json['isCompleted'] ?? false,
        bestScore: (json['bestScore'] ?? 0.0).toDouble(),
        updatedAt: json['updatedAt'] ?? '',
      );

  Map<String, dynamic> toJson() => {
        'lessonId': lessonId,
        'isCompleted': isCompleted,
        'bestScore': bestScore,
        'updatedAt': updatedAt,
      };
}
