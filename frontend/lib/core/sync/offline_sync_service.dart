import 'dart:convert';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';

class OfflineSyncAttemptDetail {
  final bool isPassed;
  final int correctCount;
  final int totalQuestions;
  final int coinsEarned;
  final double score;

  const OfflineSyncAttemptDetail({
    required this.isPassed,
    required this.correctCount,
    required this.totalQuestions,
    required this.coinsEarned,
    required this.score,
  });
}

class OfflineSyncSummary {
  final int attempts;
  final int progress;
  final int totalCoinsEarned;
  final int passedCount;
  final int failedCount;
  final List<OfflineSyncAttemptDetail> attemptDetails;

  const OfflineSyncSummary({
    required this.attempts,
    required this.progress,
    this.totalCoinsEarned = 0,
    this.passedCount = 0,
    this.failedCount = 0,
    this.attemptDetails = const [],
  });

  int get totalItems => attempts + progress;
  bool get hasSyncedData => totalItems > 0;
}

class OfflineSyncService {
  OfflineSyncService._();
  static final instance = OfflineSyncService._();
  final _db = LocalDbService();
  final Map<String, Future<OfflineSyncSummary>> _activeSyncs = {};

  Future<OfflineSyncSummary> sync(String userId) async {
    final activeSync = _activeSyncs[userId];
    if (activeSync != null) return activeSync;

    late final Future<OfflineSyncSummary> operation;
    operation = _syncInternal(userId).whenComplete(() {
      if (identical(_activeSyncs[userId], operation)) {
        _activeSyncs.remove(userId);
      }
    });
    _activeSyncs[userId] = operation;
    return operation;
  }

  Future<OfflineSyncSummary> _syncInternal(String userId) async {
    final attempts = await _db.getUnsyncedAttempts(userId);
    final progress = await _db.getUnsyncedProgress(userId);
    if (attempts.isEmpty && progress.isEmpty) return const OfflineSyncSummary(attempts: 0, progress: 0);
    final attemptIds = attempts.map((item) => item['id'] as int).toList();
    final progressIds = progress.map((item) => item['id'] as int).toList();
    final body = {
      'attempts': attempts.map((item) => {
        'quizId': item['quiz_id'],
        'lessonId': item['lesson_id'] == '' ? null : item['lesson_id'],
        'clientAttemptId': item['client_attempt_id'],
        'durationSeconds': item['duration_seconds'],
        'clientCreatedAt': item['client_created_at'],
        'answers': (jsonDecode(item['answers_json'] as String) as List<dynamic>),
      }).toList(),
      'progress': {'items': progress.map((item) => {
        'lessonId': item['lesson_id'],
        'isCompleted': false,
        'bestScore': 0,
        'clientUpdatedAt': item['client_updated_at'],
      }).toList()},
    };
    try {
      final response = await ApiClient.instance.post('/sync', data: body);
      await _db.markAttemptsSynced(attemptIds);
      await _db.markProgressSynced(progressIds);

      int totalCoins = 0;
      int passedCount = 0;
      int failedCount = 0;
      final List<OfflineSyncAttemptDetail> details = [];

      if (response.data != null && response.data['attempts'] is List) {
        for (var att in response.data['attempts']) {
          final coins = (att['coinsEarned'] ?? 0) as int;
          final isPassed = att['isPassed'] == true;
          final correct = (att['correctCount'] ?? 0) as int;
          final total = (att['totalQuestions'] ?? 0) as int;
          final score = ((att['score'] ?? 0) as num).toDouble();

          totalCoins += coins;
          if (isPassed) {
            passedCount++;
          } else {
            failedCount++;
          }
          details.add(OfflineSyncAttemptDetail(
            isPassed: isPassed,
            correctCount: correct,
            totalQuestions: total,
            coinsEarned: coins,
            score: score,
          ));
        }
      }

      return OfflineSyncSummary(
        attempts: attemptIds.length,
        progress: progressIds.length,
        totalCoinsEarned: totalCoins,
        passedCount: passedCount,
        failedCount: failedCount,
        attemptDetails: details,
      );
    } catch (error) {
      await _db.markAttemptsFailed(attemptIds, error.toString());
      rethrow;
    }
  }
}
