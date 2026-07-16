class NotificationDto {
  final String id;
  final String title;
  final String body;
  final String? link;
  final String type;
  final String? relatedEntityId;
  final bool isRead;
  final DateTime createdAt;

  NotificationDto({
    required this.id,
    required this.title,
    required this.body,
    this.link,
    required this.type,
    this.relatedEntityId,
    required this.isRead,
    required this.createdAt,
  });

  factory NotificationDto.fromJson(Map<String, dynamic> json) {
    return NotificationDto(
      id: json['id'] as String,
      title: json['title'] as String,
      body: json['body'] as String,
      link: json['link'] as String?,
      type: json['type'] as String,
      relatedEntityId: json['relatedEntityId'] as String?,
      isRead: json['isRead'] as bool,
      createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
    );
  }
}
