class NotificationModel {
  final String id;
  final String title;
  final String body;
  final String? link;
  final bool isRead;
  final String createdAt;

  NotificationModel({
    this.id = '',
    this.title = '',
    this.body = '',
    this.link,
    this.isRead = false,
    this.createdAt = '',
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) =>
      NotificationModel(
        id: json['id'] ?? '',
        title: json['title'] ?? '',
        body: json['body'] ?? '',
        link: json['link'],
        isRead: json['isRead'] ?? false,
        createdAt: json['createdAt'] ?? '',
      );
}

class UnreadCountModel {
  final int count;

  UnreadCountModel({this.count = 0});

  factory UnreadCountModel.fromJson(Map<String, dynamic> json) =>
      UnreadCountModel(count: json['count'] ?? 0);
}
