class RewardPolicyDto {
  final String id;
  final String name;
  final int quizType;
  final int coinsPerCorrectAnswer;
  final int firstPassBonusCoins;
  final int perfectScoreBonusCoins;
  final int chapterCompletionBonusCoins;
  final int retryRewardPercent;
  final int? dailyCoinLimit;
  final DateTime effectiveFrom;
  final DateTime? effectiveTo;
  final bool isActive;

  RewardPolicyDto({
    required this.id,
    required this.name,
    required this.quizType,
    required this.coinsPerCorrectAnswer,
    required this.firstPassBonusCoins,
    required this.perfectScoreBonusCoins,
    required this.chapterCompletionBonusCoins,
    required this.retryRewardPercent,
    this.dailyCoinLimit,
    required this.effectiveFrom,
    this.effectiveTo,
    required this.isActive,
  });

  factory RewardPolicyDto.fromJson(Map<String, dynamic> json) {
    return RewardPolicyDto(
      id: json['id'] as String,
      name: json['name'] as String,
      quizType: json['quizType'] as int,
      coinsPerCorrectAnswer: json['coinsPerCorrectAnswer'] as int,
      firstPassBonusCoins: json['firstPassBonusCoins'] as int,
      perfectScoreBonusCoins: json['perfectScoreBonusCoins'] as int,
      chapterCompletionBonusCoins: json['chapterCompletionBonusCoins'] as int,
      retryRewardPercent: json['retryRewardPercent'] as int,
      dailyCoinLimit: json['dailyCoinLimit'] as int?,
      effectiveFrom: DateTime.parse(json['effectiveFrom'] as String).toLocal(),
      effectiveTo: json['effectiveTo'] != null ? DateTime.parse(json['effectiveTo'] as String).toLocal() : null,
      isActive: json['isActive'] as bool,
    );
  }
}
