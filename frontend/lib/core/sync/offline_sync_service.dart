import 'dart:convert';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';

class OfflineSyncAttemptDetail {
  // Chi tiết do server chấm, chỉ dùng trình bày dialog sau đồng bộ.
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
  // Summary tách khỏi JSON API để StudentShell/OfflineSyncScreen dùng thống nhất.
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
  // Progress không có quiz vẫn được coi là dữ liệu đã đồng bộ.
  bool get hasSyncedData => totalItems > 0;
}

class OfflineSyncService {
  OfflineSyncService._();
  static final instance = OfflineSyncService._();
  final _db = LocalDbService();
  // Map userId → Future đang chạy ngăn StudentShell và OfflineSyncScreen gửi hai request.
  final Map<String, Future<OfflineSyncSummary>> _activeSyncs = {};

  Future<OfflineSyncSummary> sync(String userId) async {
    // Nếu user này đang sync, caller mới chờ cùng kết quả thay vì POST lần nữa.
    final activeSync = _activeSyncs[userId];
    if (activeSync != null) return activeSync;

    late final Future<OfflineSyncSummary> operation;
    // whenComplete luôn dọn map dù request thành công hay ném lỗi.
    operation = _syncInternal(userId).whenComplete(() {
      if (identical(_activeSyncs[userId], operation)) {
        _activeSyncs.remove(userId);
      }
    });
    _activeSyncs[userId] = operation;
    return operation;
  }

  Future<OfflineSyncSummary> _syncInternal(String userId) async {
    // Đọc song song hai loại queue logic cho đúng user hiện tại.
    final attempts = await _db.getUnsyncedAttempts(userId);
    final progress = await _db.getUnsyncedProgress(userId);
    // Queue rỗng không cần gọi server nhưng vẫn trả summary hợp lệ cho UI.
    if (attempts.isEmpty && progress.isEmpty)
      return const OfflineSyncSummary(attempts: 0, progress: 0);
    // Giữ local ids riêng để mark đúng record sau response.
    final attemptIds = attempts.map((item) => item['id'] as int).toList();
    final progressIds = progress.map((item) => item['id'] as int).toList();
    // Chuyển tên cột snake_case SQLite sang JSON camelCase của C# DTO.
    final body = {
      'attempts': attempts
          .map(
            (item) => {
              'quizId': item['quiz_id'],
              'lessonId': item['lesson_id'] == '' ? null : item['lesson_id'],
              // clientAttemptId phải giữ nguyên qua mọi lần retry để server nhận ra duplicate.
              'clientAttemptId': item['client_attempt_id'],
              'durationSeconds': item['duration_seconds'],
              'clientCreatedAt': item['client_created_at'],
              // Decode answers_json trở lại List trước khi Dio encode toàn body.
              'answers':
                  (jsonDecode(item['answers_json'] as String) as List<dynamic>),
            },
          )
          .toList(),
      'progress': {
        'items': progress
            .map(
              (item) => {
                'lessonId': item['lesson_id'],
                // Client không tự khai completed/bestScore; server suy ra từ QuizAttempts.
                'isCompleted': false,
                'bestScore': 0,
                'clientUpdatedAt': item['client_updated_at'],
              },
            )
            .toList(),
      },
    };
    try {
      // [NGOẠI TUYẾN] Bước 6: Nhân viên giao liên - Lấy bài từ SQLite và gửi tất cả lên Backend (Server) chấm điểm.
      final response = await ApiClient.instance.post('/sync', data: body);
      // [NGOẠI TUYẾN] Bước 7: Dọn dẹp - Sau khi Server báo 200 OK (đã nhận và chấm xong), xóa sạch bài tạm trong SQLite để không gửi trùng.
      await _db.markAttemptsSynced(attemptIds);
      await _db.markProgressSynced(progressIds);

      // Tổng hợp response để dialog có số bài đạt, điểm và xu.
      int totalCoins = 0;
      int passedCount = 0;
      int failedCount = 0;
      final List<OfflineSyncAttemptDetail> details = [];

      if (response.data != null && response.data['attempts'] is List) {
        for (var att in response.data['attempts']) {
          // Dùng giá trị mặc định để client không crash nếu server thiếu field tùy chọn.
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
          details.add(
            OfflineSyncAttemptDetail(
              isPassed: isPassed,
              correctCount: correct,
              totalQuestions: total,
              coinsEarned: coins,
              score: score,
            ),
          );
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
      // Không xóa queue; ghi lỗi và rethrow để UI hiện thông báo thất bại.
      await _db.markAttemptsFailed(attemptIds, error.toString());
      rethrow;
    }
  }
}
