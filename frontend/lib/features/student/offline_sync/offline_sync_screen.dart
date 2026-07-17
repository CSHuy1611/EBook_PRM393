import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';
import 'package:math_ibook/core/sync/offline_sync_service.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';

class OfflineSyncScreen extends StatefulWidget {
  const OfflineSyncScreen({super.key});
  @override
  State<OfflineSyncScreen> createState() => _OfflineSyncScreenState();
}

class _OfflineSyncScreenState extends State<OfflineSyncScreen> {
  final _db = LocalDbService();
  final _connectivity = Connectivity();
  StreamSubscription<List<ConnectivityResult>>? _subscription;
  bool _online = false;
  bool? _wasOnline;
  bool _syncing = false;
  List<Map<String, dynamic>> _attempts = [];
  List<Map<String, dynamic>> _progress = [];

  String get _userId => context.read<AuthProvider>().currentUser?.id ?? '';

  @override
  void initState() { super.initState(); _start(); }
  @override
  void dispose() { _subscription?.cancel(); super.dispose(); }

  Future<void> _start() async {
    final result = await _connectivity.checkConnectivity();
    if (!mounted) return;
    _setConnectivity(result);
    _subscription = _connectivity.onConnectivityChanged.listen(_setConnectivity);
    await _reload();
  }

  void _setConnectivity(List<ConnectivityResult> result) {
    if (!mounted) return;
    final online = result.any((item) => item != ConnectivityResult.none);
    final reconnected = _wasOnline == false && online;
    _wasOnline = online;
    setState(() => _online = online);
    if (reconnected) unawaited(_sync(automatic: true));
  }

  Future<void> _reload() async {
    if (_userId.isEmpty || !_db.isAvailable) return;
    final values = await Future.wait([_db.getUnsyncedAttempts(_userId), _db.getUnsyncedProgress(_userId)]);
    if (mounted) setState(() { _attempts = values[0]; _progress = values[1]; });
  }

  Future<void> _sync({bool automatic = false}) async {
    if (!_online || _syncing || _userId.isEmpty) return;
    setState(() => _syncing = true);
    try {
      final summary = await OfflineSyncService.instance.sync(_userId);
      if (!mounted) return;
      if (summary.hasSyncedData) {
        context.read<ProgressNotifier>().notifyProgressChanged();
      }
      final message = summary.hasSyncedData
          ? 'Đã đồng bộ ${summary.attempts} bài làm và ${summary.progress} tiến độ.'
          : automatic
              ? 'Đã kết nối lại mạng. Không có dữ liệu chờ đồng bộ.'
              : 'Không có dữ liệu chờ đồng bộ.';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: summary.hasSyncedData ? Colors.green : null,
        ),
      );
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Đã có mạng nhưng chưa thể đồng bộ. Dữ liệu vẫn được giữ lại để thử lại.',
            ),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      await _reload();
      if (mounted) setState(() => _syncing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!_db.isAvailable) return const Scaffold(body: Center(child: Text('Đồng bộ offline hiện hỗ trợ trên ứng dụng di động và desktop.')));
    final count = _attempts.length + _progress.length;
    return Scaffold(
      appBar: AppBar(title: const Text('Ngoại tuyến và đồng bộ')),
      body: RefreshIndicator(
        onRefresh: _reload,
        child: ListView(padding: const EdgeInsets.all(16), children: [
          Card(child: ListTile(
            leading: Icon(_online ? Icons.cloud_done : Icons.cloud_off, color: _online ? Colors.green : Colors.orange, size: 34),
            title: Text(_online ? 'Đã có kết nối mạng' : 'Đang ở chế độ ngoại tuyến'),
            subtitle: Text(count == 0 ? 'Không có dữ liệu chờ đồng bộ.' : '$count mục đang chờ đồng bộ.'),
          )),
          const SizedBox(height: 16),
          FilledButton.icon(onPressed: _online && count > 0 && !_syncing ? _sync : null, icon: _syncing ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.sync), label: Text(_syncing ? 'Đang đồng bộ...' : 'Đồng bộ ngay')),
          const SizedBox(height: 24),
          if (_attempts.isNotEmpty) ...[
            Text('Quiz chờ xác nhận (${_attempts.length})', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            ..._attempts.map((item) => Card(child: ListTile(leading: const Icon(Icons.quiz_outlined), title: const Text('Bài quiz ngoại tuyến'), subtitle: Text(item['last_sync_error'] as String? ?? 'Chờ server chấm điểm, cộng xu và xét huy hiệu.'), trailing: const Icon(Icons.schedule)))),
          ],
          if (_progress.isNotEmpty) ...[
            const SizedBox(height: 16), Text('Tiến độ chờ đồng bộ (${_progress.length})', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            ..._progress.map((_) => const Card(child: ListTile(leading: Icon(Icons.menu_book_outlined), title: Text('Đã xem bài học'), subtitle: Text('Chờ đồng bộ tiến độ')))),
          ],
        ]),
      ),
    );
  }
}
