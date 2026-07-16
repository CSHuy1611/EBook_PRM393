class CoinTransactionModel {
  final int amount;
  final String sourceType;
  final String description;
  final int balanceAfter;
  final DateTime createdAt;

  const CoinTransactionModel({
    required this.amount,
    required this.sourceType,
    required this.description,
    required this.balanceAfter,
    required this.createdAt,
  });

  factory CoinTransactionModel.fromJson(Map<String, dynamic> json) =>
      CoinTransactionModel(
        amount: json['amount'] as int? ?? 0,
        sourceType: json['sourceType'] as String? ?? '',
        description: json['description'] as String? ?? '',
        balanceAfter: json['balanceAfter'] as int? ?? 0,
        createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
            DateTime.fromMillisecondsSinceEpoch(0),
      );
}

class CoinHistoryModel {
  final int totalCoins;
  final int page;
  final int pageSize;
  final int totalItems;
  final List<CoinTransactionModel> items;

  const CoinHistoryModel({
    required this.totalCoins,
    required this.page,
    required this.pageSize,
    required this.totalItems,
    required this.items,
  });

  factory CoinHistoryModel.fromJson(Map<String, dynamic> json) => CoinHistoryModel(
        totalCoins: json['totalCoins'] as int? ?? 0,
        page: json['page'] as int? ?? 1,
        pageSize: json['pageSize'] as int? ?? 20,
        totalItems: json['totalItems'] as int? ?? 0,
        items: (json['items'] as List<dynamic>? ?? [])
            .map((item) => CoinTransactionModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
}

class BadgeProgressRuleModel {
  final String requirement;
  final int currentValue;
  final int targetValue;
  final double percentage;

  const BadgeProgressRuleModel({
    required this.requirement,
    required this.currentValue,
    required this.targetValue,
    required this.percentage,
  });

  factory BadgeProgressRuleModel.fromJson(Map<String, dynamic> json) =>
      BadgeProgressRuleModel(
        requirement: json['requirement'] as String? ?? '',
        currentValue: json['currentValue'] as int? ?? 0,
        targetValue: json['targetValue'] as int? ?? 0,
        percentage: (json['percentage'] as num? ?? 0).toDouble(),
      );
}

class BadgeCollectionItemModel {
  final String id;
  final String title;
  final String description;
  final String iconUrl;
  final String status;
  final DateTime? earnedAt;
  final double progressPercentage;
  final String requirement;
  final int currentValue;
  final int targetValue;
  final List<BadgeProgressRuleModel> rules;

  const BadgeCollectionItemModel({
    required this.id,
    required this.title,
    required this.description,
    required this.iconUrl,
    required this.status,
    required this.earnedAt,
    required this.progressPercentage,
    required this.requirement,
    required this.currentValue,
    required this.targetValue,
    required this.rules,
  });

  factory BadgeCollectionItemModel.fromJson(Map<String, dynamic> json) =>
      BadgeCollectionItemModel(
        id: json['id'] as String? ?? '',
        title: json['title'] as String? ?? '',
        description: json['description'] as String? ?? '',
        iconUrl: json['iconUrl'] as String? ?? '',
        status: json['status'] as String? ?? 'Locked',
        earnedAt: json['earnedAt'] == null
            ? null
            : DateTime.tryParse(json['earnedAt'] as String),
        progressPercentage: (json['progressPercentage'] as num? ?? 0).toDouble(),
        requirement: json['requirement'] as String? ?? '',
        currentValue: json['currentValue'] as int? ?? 0,
        targetValue: json['targetValue'] as int? ?? 0,
        rules: (json['rules'] as List<dynamic>? ?? [])
            .map((rule) => BadgeProgressRuleModel.fromJson(rule as Map<String, dynamic>))
            .toList(),
      );
}

class BadgeCollectionModel {
  final int earnedCount;
  final int totalCount;
  final List<BadgeCollectionItemModel> items;

  const BadgeCollectionModel({
    required this.earnedCount,
    required this.totalCount,
    required this.items,
  });

  factory BadgeCollectionModel.fromJson(Map<String, dynamic> json) =>
      BadgeCollectionModel(
        earnedCount: json['earnedCount'] as int? ?? 0,
        totalCount: json['totalCount'] as int? ?? 0,
        items: (json['items'] as List<dynamic>? ?? [])
            .map((item) => BadgeCollectionItemModel.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
}

class LeaderboardEntryModel {
  final int rank;
  final String userId;
  final String name;
  final String? avatarUrl;
  final int coins;
  final int badgeCount;
  final bool isCurrentUser;

  const LeaderboardEntryModel({
    required this.rank,
    required this.userId,
    required this.name,
    this.avatarUrl,
    required this.coins,
    required this.badgeCount,
    required this.isCurrentUser,
  });

  factory LeaderboardEntryModel.fromJson(Map<String, dynamic> json) =>
      LeaderboardEntryModel(
        rank: json['rank'] as int? ?? 0,
        userId: json['userId'] as String? ?? '',
        name: json['name'] as String? ?? '',
        avatarUrl: json['avatarUrl'] as String?,
        coins: json['coins'] as int? ?? 0,
        badgeCount: json['badgeCount'] as int? ?? 0,
        isCurrentUser: json['isCurrentUser'] as bool? ?? false,
      );
}

class LeaderboardModel {
  final List<LeaderboardEntryModel> top100;
  final LeaderboardEntryModel? currentUser;
  final DateTime updatedAt;

  const LeaderboardModel({
    required this.top100,
    required this.currentUser,
    required this.updatedAt,
  });

  factory LeaderboardModel.fromJson(Map<String, dynamic> json) => LeaderboardModel(
        top100: (json['top100'] as List<dynamic>? ?? [])
            .map((item) => LeaderboardEntryModel.fromJson(item as Map<String, dynamic>))
            .toList(),
        currentUser: json['currentUser'] is Map<String, dynamic>
            ? LeaderboardEntryModel.fromJson(json['currentUser'] as Map<String, dynamic>)
            : null,
        updatedAt: DateTime.tryParse(json['updatedAt'] as String? ?? '') ?? DateTime.now(),
      );
}

class StudentProfileModel {
  final String id;
  final String name;
  final String email;
  final String? avatarUrl;
  final int coins;
  final int badgeCount;
  final int? rank;
  final int completedLessons;
  final int completedChapters;
  final double averageScore;
  final double bestScore;

  const StudentProfileModel({
    required this.id,
    required this.name,
    required this.email,
    this.avatarUrl,
    required this.coins,
    required this.badgeCount,
    this.rank,
    required this.completedLessons,
    required this.completedChapters,
    required this.averageScore,
    required this.bestScore,
  });

  factory StudentProfileModel.fromJson(Map<String, dynamic> json) => StudentProfileModel(
        id: json['id'] as String? ?? '',
        name: json['name'] as String? ?? '',
        email: json['email'] as String? ?? '',
        avatarUrl: json['avatarUrl'] as String?,
        coins: json['coins'] as int? ?? 0,
        badgeCount: json['badgeCount'] as int? ?? 0,
        rank: json['rank'] as int?,
        completedLessons: json['completedLessons'] as int? ?? 0,
        completedChapters: json['completedChapters'] as int? ?? 0,
        averageScore: (json['averageScore'] as num? ?? 0).toDouble(),
        bestScore: (json['bestScore'] as num? ?? 0).toDouble(),
      );
}
