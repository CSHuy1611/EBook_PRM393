class BadgeRuleDto {
  final String id;
  final String ruleType;
  final String? targetChapterId;
  final int? thresholdValue;
  
  BadgeRuleDto({
    required this.id,
    required this.ruleType,
    this.targetChapterId,
    this.thresholdValue,
  });

  factory BadgeRuleDto.fromJson(Map<String, dynamic> json) => BadgeRuleDto(
    id: json['id'] ?? '',
    ruleType: json['ruleType'] ?? '',
    targetChapterId: json['targetChapterId'],
    thresholdValue: json['thresholdValue'],
  );
}

class BadgeDto {
  final String id;
  final String title;
  final String description;
  final String iconUrl;
  final List<BadgeRuleDto> rules;
  final bool isActive;

  BadgeDto({
    required this.id,
    required this.title,
    this.description = '',
    this.iconUrl = '',
    this.rules = const [],
    this.isActive = true,
  });

  factory BadgeDto.fromJson(Map<String, dynamic> json) {
    var rulesList = <BadgeRuleDto>[];
    if (json['rules'] != null && json['rules'] is List) {
      rulesList = (json['rules'] as List).map((e) => BadgeRuleDto.fromJson(e)).toList();
    }
    return BadgeDto(
      id: json['id'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      iconUrl: json['iconUrl'] ?? json['icon_url'] ?? '',
      rules: rulesList,
      isActive: json['isActive'] ?? true,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'description': description,
        'iconUrl': iconUrl,
        'rules': rules.map((e) => {
          'id': e.id,
          'ruleType': e.ruleType,
          'targetChapterId': e.targetChapterId,
          'thresholdValue': e.thresholdValue,
        }).toList(),
        'isActive': isActive,
      };
}

class BadgeCreateDto {
  final String title;
  final String description;
  final String iconUrl;
  final String conditionType;
  final String conditionValue;

  BadgeCreateDto({
    required this.title,
    this.description = '',
    this.iconUrl = '',
    this.conditionType = '',
    this.conditionValue = '',
  });

  factory BadgeCreateDto.fromJson(Map<String, dynamic> json) =>
      BadgeCreateDto(
        title: json['title'] ?? '',
        description: json['description'] ?? '',
        iconUrl: json['iconUrl'] ?? '',
        conditionType: json['conditionType'] ?? '',
        conditionValue: json['conditionValue']?.toString() ?? '',
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
  final String? conditionValue;

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
        conditionValue: json['conditionValue']?.toString(),
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
