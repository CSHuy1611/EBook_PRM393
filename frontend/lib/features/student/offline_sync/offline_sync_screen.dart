import 'dart:async';
import 'package:flutter/material.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/models/progress_models.dart';

class OfflineSyncScreen extends StatefulWidget {
  const OfflineSyncScreen({super.key});

  @override
  State<OfflineSyncScreen> createState() => _OfflineSyncScreenState();
}

class _OfflineSyncScreenState extends State<OfflineSyncScreen> {
  final Connectivity _connectivity = Connectivity();
  final LocalDbService _dbService = LocalDbService();

  bool _isOnline = true;
  bool _isSyncing = false;
  List<Map<String, dynamic>> _unsyncedProgress = [];
  List<Map<String, dynamic>> _unsyncedAttempts = [];
  StreamSubscription? _connectivitySub;

  @override
  void initState() {
    super.initState();
    _initConnectivity();
    _loadUnsynced();
  }

  @override
  void dispose() {
    _connectivitySub?.cancel();
    super.dispose();
  }

  Future<void> _initConnectivity() async {
    final result = await _connectivity.checkConnectivity();
    _updateConnectionStatus(result);
    _connectivitySub = _connectivity.onConnectivityChanged.listen(_updateConnectionStatus);
  }

  void _updateConnectionStatus(List<ConnectivityResult> results) {
    if (!mounted) return;
    setState(() {
      _isOnline = results.any((r) => r != ConnectivityResult.none);
    });
    if (_isOnline) {
      _loadUnsynced();
    }
  }

  Future<void> _loadUnsynced() async {
    final progress = await _dbService.getUnsyncedProgress();
    final attempts = await _dbService.getUnsyncedAttempts();
    if (mounted) {
      setState(() {
        _unsyncedProgress = progress;
        _unsyncedAttempts = attempts;
      });
    }
  }

  Future<void> _syncAll() async {
    if (!_isOnline) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không có kết nối mạng. Vui lòng thử lại sau.')),
      );
      return;
    }

    setState(() => _isSyncing = true);

    try {
      int synced = 0;

      for (final progress in _unsyncedProgress) {
        try {
          await ApiClient.instance.post('/progress/sync', data: {
            'lessonId': progress['lesson_id'],
            'isCompleted': progress['is_completed'],
            'bestScore': progress['best_score'],
            'clientUpdatedAt': progress['client_updated_at'],
          });
          await _dbService.markProgressSynced(progress['id'] as int);
          synced++;
        } catch (_) {
          // skip failed item
        }
      }

      for (final attempt in _unsyncedAttempts) {
        try {
          String? answersStr = attempt['answers'] as String?;
          dynamic answersData = answersStr != null ? answersStr : [];
          await ApiClient.instance.post('/quiz-attempts', data: {
            'lessonId': attempt['lesson_id'],
            'score': attempt['score'],
            'totalQuestions': attempt['total_questions'],
            'durationSeconds': attempt['duration_seconds'],
            'answers': answersData,
            'clientCreatedAt': attempt['client_created_at'],
          });
          await _dbService.markAttemptSynced(attempt['id'] as int);
          synced++;
        } catch (_) {
          // skip failed item
        }
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Đã đồng bộ $synced mục thành công')),
        );
        await _loadUnsynced();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi đồng bộ: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isSyncing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final totalUnsynced = _unsyncedProgress.length + _unsyncedAttempts.length;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Đồng bộ hóa'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Icon(
                      _isOnline ? Icons.wifi : Icons.wifi_off,
                      color: _isOnline ? Colors.green : Colors.red,
                      size: 32,
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            _isOnline ? 'Đã kết nối' : 'Mất kết nối',
                            style: TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                              color: _isOnline ? Colors.green : Colors.red,
                            ),
                          ),
                          Text(
                            _isOnline
                                ? 'Sẵn sàng đồng bộ'
                                : 'Đang ở chế độ ngoại tuyến',
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                    if (_isOnline && totalUnsynced > 0)
                      SizedBox(
                        width: 48,
                        height: 48,
                        child: Stack(
                          fit: StackFit.expand,
                          children: [
                            CircularProgressIndicator(
                              value: _isSyncing ? null : 1.0,
                              strokeWidth: 4,
                            ),
                            Center(child: Text('$totalUnsynced', style: const TextStyle(fontWeight: FontWeight.bold))),
                          ],
                        ),
                      ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: FilledButton.icon(
                    onPressed: (_isOnline && totalUnsynced > 0 && !_isSyncing) ? _syncAll : null,
                    icon: _isSyncing
                        ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : const Icon(Icons.sync),
                    label: Text(_isSyncing ? 'Đang đồng bộ...' : 'Đồng bộ hóa'),
                    style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            Text(
              'Chi tiết',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            if (_isSyncing)
              const LinearProgressIndicator()
            else if (totalUnsynced == 0)
              Expanded(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.cloud_done, size: 64, color: Colors.green.shade300),
                      const SizedBox(height: 16),
                      const Text('Tất cả dữ liệu đã được đồng bộ', style: TextStyle(fontSize: 16)),
                    ],
                  ),
                ),
              )
            else
              Expanded(
                child: ListView(
                  children: [
                    if (_unsyncedProgress.isNotEmpty) ...[
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: Text('Tiến độ chưa đồng bộ (${_unsyncedProgress.length})',
                            style: const TextStyle(fontWeight: FontWeight.w600)),
                      ),
                      ..._unsyncedProgress.map((item) => ListTile(
                            leading: const Icon(Icons.menu_book),
                            title: Text('Bài học: ${item['lesson_id']}'),
                            subtitle: Text('Hoàn thành: ${item['is_completed'] == 1 ? "Có" : "Không"}'),
                            dense: true,
                          )),
                    ],
                    if (_unsyncedAttempts.isNotEmpty) ...[
                      Padding(
                        padding: const EdgeInsets.only(top: 8, bottom: 8),
                        child: Text('Bài kiểm tra chưa đồng bộ (${_unsyncedAttempts.length})',
                            style: const TextStyle(fontWeight: FontWeight.w600)),
                      ),
                      ..._unsyncedAttempts.map((item) => ListTile(
                            leading: const Icon(Icons.quiz),
                            title: Text('Bài kiểm tra: ${item['lesson_id']}'),
                            subtitle: Text('Điểm: ${item['score']}/${item['total_questions']}'),
                            dense: true,
                          )),
                    ],
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
