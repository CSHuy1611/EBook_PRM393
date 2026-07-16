import 'dart:convert';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';

class OfflineSyncSummary {
  final int attempts;
  final int progress;
  const OfflineSyncSummary({required this.attempts, required this.progress});
}

class OfflineSyncService {
  OfflineSyncService._();
  static final instance = OfflineSyncService._();
  final _db = LocalDbService();

  Future<OfflineSyncSummary> sync(String userId) async {
    final attempts = await _db.getUnsyncedAttempts(userId);
    final progress = await _db.getUnsyncedProgress(userId);
    if (attempts.isEmpty && progress.isEmpty) return const OfflineSyncSummary(attempts: 0, progress: 0);
    final attemptIds = attempts.map((item) => item['id'] as int).toList();
    final progressIds = progress.map((item) => item['id'] as int).toList();
    final body = {
      'attempts': attempts.map((item) => {
        'quizId': item['quiz_id'],
        'lessonId': item['lesson_id'],
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
      await ApiClient.instance.post('/sync', data: body);
      await _db.markAttemptsSynced(attemptIds);
      await _db.markProgressSynced(progressIds);
      return OfflineSyncSummary(attempts: attemptIds.length, progress: progressIds.length);
    } catch (error) {
      await _db.markAttemptsFailed(attemptIds, error.toString());
      rethrow;
    }
  }
}
