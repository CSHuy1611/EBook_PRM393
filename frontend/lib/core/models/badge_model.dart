class BadgeDto {
  final String id;
  final String title;
  final String description;
  final String iconUrl;
  final String conditionType;
  final int conditionValue;

  BadgeDto({
    required this.id,
    required this.title,
    this.description = '',
    this.iconUrl = '',
    this.conditionType = '',
    this.conditionValue = 0,
  });

  factory BadgeDto.fromJson(Map<String, dynamic> json) => BadgeDto(
        id: json['id'] ?? '',
        title: json['title'] ?? '',
        description: json['description'] ?? '',
        iconUrl: json['iconUrl'] ?? json['icon_url'] ?? '',
        conditionType: json['conditionType'] ?? '',
        conditionValue: json['conditionValue'] is String
            ? int.tryParse(json['conditionValue'] as String) ?? 0
            : (json['conditionValue'] as int? ?? 0),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'description': description,
        'iconUrl': iconUrl,
        'conditionType': conditionType,
        'conditionValue': conditionValue,
      };
}

class BadgeCreateDto {
  final String title;
  final String description;
  final String iconUrl;
  final String conditionType;
  final int conditionValue;

  BadgeCreateDto({
    required this.title,
    this.description = '',
    this.iconUrl = '',
    this.conditionType = '',
    this.conditionValue = 0,
  });

  factory BadgeCreateDto.fromJson(Map<String, dynamic> json) =>
      BadgeCreateDto(
        title: json['title'] ?? '',
        description: json['description'] ?? '',
        iconUrl: json['iconUrl'] ?? '',
        conditionType: json['conditionType'] ?? '',
        conditionValue: json['conditionValue'] is String
            ? int.tryParse(json['conditionValue'] as String) ?? 0
            : (json['conditionValue'] as int? ?? 0),
      );

  Map<String, dynamic> toJson() => {
        'title': title,
        'description': description,
        'iconUrl': iconUrl,
        'conditionType': conditionType,
        'conditionValue': conditionValue,
      };
}

class BadgeUpdateDto {
  final String? title;
  final String? description;
  final String? iconUrl;
  final String? conditionType;
  final int? conditionValue;

  BadgeUpdateDto({
    this.title,
    this.description,
    this.iconUrl,
    this.conditionType,
    this.conditionValue,
  });

  factory BadgeUpdateDto.fromJson(Map<String, dynamic> json) =>
      BadgeUpdateDto(
        title: json['title'],
        description: json['description'],
        iconUrl: json['iconUrl'],
        conditionType: json['conditionType'],
        conditionValue: json['conditionValue'] is String
            ? int.tryParse(json['conditionValue'] as String)
            : (json['conditionValue'] as int?),
      );

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{};
    if (title != null) map['title'] = title;
    if (description != null) map['description'] = description;
    if (iconUrl != null) map['iconUrl'] = iconUrl;
    if (conditionType != null) map['conditionType'] = conditionType;
    if (conditionValue != null) map['conditionValue'] = conditionValue;
    return map;
  }
}
