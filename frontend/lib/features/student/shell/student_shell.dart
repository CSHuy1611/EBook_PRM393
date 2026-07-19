import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/sync/offline_sync_service.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';

class StudentShell extends StatefulWidget {
  final StatefulNavigationShell navigationShell;

  const StudentShell({super.key, required this.navigationShell});

  @override
  State<StudentShell> createState() => _StudentShellState();
}

class _StudentShellState extends State<StudentShell> {
  // Badge số đỏ trên icon thông báo.
  int _unreadCount = 0;
  // Ghi version ProgressNotifier gần nhất để tránh fetch unread lặp trong build.
  int _lastVersion = -1;
  Timer? _timer;
  StreamSubscription<List<ConnectivityResult>>? _connectivitySubscription;
  // null ở startup, false khi đã offline, true khi đang online.
  bool? _wasOnline;
  // Khóa cục bộ ngăn nhiều event connectivity chạy _syncPending đồng thời.
  bool _syncing = false;

  @override
  void initState() {
    super.initState();
    _fetchUnread();
    // Refresh số thông báo mỗi 30 giây trong khi StudentShell tồn tại.
    _timer = Timer.periodic(const Duration(seconds: 30), (_) => _fetchUnread());
    // StudentShell tồn tại xuyên suốt bốn tab nên là nơi phù hợp auto-sync toàn cục.
    _initConnectivity();
  }

  Future<void> _initConnectivity() async {
    final conn = Connectivity();
    // Nghe các lần Wi-Fi/mobile/ethernet thay đổi sau startup.
    _connectivitySubscription = conn.onConnectivityChanged.listen(
      _onConnectivityChanged,
    );
    // checkConnectivity xử lý trạng thái ngay lúc mở app, trước khi stream phát event mới.
    final initial = await conn.checkConnectivity();
    if (mounted) _onConnectivityChanged(initial);
  }

  void _onConnectivityChanged(List<ConnectivityResult> result) {
    // [NGOẠI TUYẾN] Bước 4: Người gác cổng - Theo dõi khi mạng kết nối lại.
    final online = result.any((r) => r != ConnectivityResult.none);
    // Chỉ coi là reconnect khi trạng thái mạng vừa chuyển từ offline sang online.
    final justReconnected = _wasOnline == false && online;
    _wasOnline = online;

    // Mất mạng không thể sync; queue vẫn nằm nguyên trong SQLite.
    if (!online) return;

    if (justReconnected && mounted) {
      // [NGOẠI TUYẾN] Bước 5: Kích hoạt đồng bộ - Mạng đã có lại, thông báo cho học sinh biết.
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(
            content: Text(
              'Đã kết nối lại mạng. Đang kiểm tra dữ liệu chờ đồng bộ...',
            ),
            duration: Duration(seconds: 2),
          ),
        );
    }

    // Luôn thử sync khi online; chạy sau frame để tránh gọi Scaffold/setState trong build.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _syncPending(isReconnect: justReconnected);
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    _connectivitySubscription?.cancel();
    super.dispose();
  }

  Future<void> _syncPending({bool isReconnect = false}) async {
    // Bỏ qua nếu tác vụ trước chưa xong hoặc shell đã bị dispose.
    if (_syncing || !mounted) return;
    // Queue được phân vùng theo đúng userId đang đăng nhập.
    final userId = context.read<AuthProvider>().currentUser?.id ?? '';
    if (userId.isEmpty) return;

    _syncing = true;
    try {
      // Đợi ngắn sau reconnect để interface mạng có thời gian nhận IP/DNS.
      if (isReconnect) await Future.delayed(const Duration(milliseconds: 1500));
      if (!mounted) return;

      debugPrint(
        '[SYNC] Starting sync for user: $userId, isReconnect: $isReconnect',
      );
      // OfflineSyncService tiếp tục chống sync trùng nếu OfflineSyncScreen cũng gọi.
      final summary = await OfflineSyncService.instance.sync(userId);
      debugPrint(
        '[SYNC] Result: attempts=${summary.attempts}, progress=${summary.progress}, details=${summary.attemptDetails.length}',
      );
      if (!mounted) return;

      // Cả attempt lẫn progress đều là dữ liệu cần phát tín hiệu refresh.
      final hasData = summary.attempts > 0 || summary.progress > 0;

      if (hasData) {
        // Leaderboard/notifications và các consumer khác có thể tải lại dữ liệu server.
        context.read<ProgressNotifier>().notifyProgressChanged();
        ScaffoldMessenger.of(context).hideCurrentSnackBar();
        debugPrint('[SYNC] Showing dialog...');
        // Dialog chỉ xuất hiện khi thực sự có dữ liệu vừa đồng bộ.
        await _showSyncSuccessDialog(summary);
        // Reconnect nhưng queue rỗng vẫn có phản hồi, tránh cảm giác app không hoạt động.
      } else if (isReconnect && mounted) {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            const SnackBar(
              content: Text('Đã có mạng. Không có dữ liệu chờ đồng bộ.'),
            ),
          );
      }
    } catch (e) {
      debugPrint('[SYNC] Error: $e');
      if (!mounted) return;
      // Chỉ chủ động báo lỗi trong luồng reconnect; startup online tránh gây SnackBar nhiễu.
      if (isReconnect) {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            const SnackBar(
              content: Text(
                'Có mạng nhưng chưa đồng bộ được. Dữ liệu vẫn được giữ lại.',
              ),
              backgroundColor: Colors.red,
            ),
          );
      }
    } finally {
      // Mở khóa cho event mạng hoặc thao tác sync tiếp theo.
      _syncing = false;
    }
  }

  Future<void> _showSyncSuccessDialog(OfflineSyncSummary summary) async {
    if (!mounted) return;
    debugPrint(
      '[SYNC] _showSyncSuccessDialog called, attempts=${summary.attempts}',
    );
    await showDialog<void>(
      context: context,
      // Root navigator giúp dialog nằm trên StatefulShellRoute và mọi tab.
      useRootNavigator: true,
      // Buộc người dùng xác nhận kết quả, tránh dialog tự mất khi chuyển tab.
      barrierDismissible: false,
      builder: (ctx) {
        final colors = Theme.of(ctx).colorScheme;
        // Dùng Column các row; ListView lồng AlertDialog có thể gặp unbounded height.
        final detailWidgets = summary.attemptDetails.asMap().entries.map((
          entry,
        ) {
          final index = entry.key;
          final d = entry.value;
          return Container(
            decoration: BoxDecoration(
              border: Border(
                bottom: BorderSide(
                  color: colors.outlineVariant.withAlpha(80),
                  width: index < summary.attemptDetails.length - 1 ? 1 : 0,
                ),
              ),
            ),
            padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 4),
            child: Row(
              children: [
                Icon(
                  d.isPassed ? Icons.check_circle : Icons.cancel,
                  color: d.isPassed ? Colors.green : colors.error,
                  size: 20,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Bài ${index + 1}: Đúng ${d.correctCount}/${d.totalQuestions} câu',
                        style: const TextStyle(
                          fontWeight: FontWeight.w500,
                          fontSize: 13,
                        ),
                      ),
                      Text(
                        'Điểm: ${d.score.toStringAsFixed(1)}'
                        '${d.coinsEarned > 0 ? ' • +${d.coinsEarned} xu' : ''}',
                        style: TextStyle(
                          fontSize: 12,
                          color: colors.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          );
        }).toList();

        return AlertDialog(
          title: Row(
            children: [
              const Icon(Icons.cloud_done_rounded, color: Colors.green),
              const SizedBox(width: 10),
              const Expanded(
                child: Text(
                  'Đồng bộ thành công!',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Colors.green,
                  ),
                ),
              ),
            ],
          ),
          contentPadding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
          content: SizedBox(
            width: double.maxFinite,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (summary.attempts > 0) ...[
                    Text(
                      'Hệ thống đã chấm điểm ${summary.attempts} bài làm ngoại tuyến.',
                    ),
                    const SizedBox(height: 12),
                  ],
                  if (detailWidgets.isNotEmpty) ...[
                    Container(
                      decoration: BoxDecoration(
                        border: Border.all(color: colors.outlineVariant),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: detailWidgets,
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        const Icon(
                          Icons.check_circle,
                          color: Colors.green,
                          size: 15,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          'Đạt: ${summary.passedCount} bài',
                          style: const TextStyle(fontSize: 13),
                        ),
                        const SizedBox(width: 16),
                        Icon(Icons.cancel, color: colors.error, size: 15),
                        const SizedBox(width: 4),
                        Text(
                          'Chưa đạt: ${summary.failedCount} bài',
                          style: const TextStyle(fontSize: 13),
                        ),
                      ],
                    ),
                  ] else if (summary.attempts > 0) ...[
                    Row(
                      children: [
                        const Text('Đã Đạt: '),
                        Text('${summary.passedCount} bài'),
                        const SizedBox(width: 12),
                        const Text('❌ Chưa đạt: '),
                        Text('${summary.failedCount} bài'),
                      ],
                    ),
                  ],
                  if (summary.totalCoinsEarned > 0) ...[
                    const SizedBox(height: 12),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.orange.withAlpha(25),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.orange.withAlpha(80)),
                      ),
                      child: Row(
                        children: [
                          const Text('💰', style: TextStyle(fontSize: 18)),
                          const SizedBox(width: 8),
                          Text(
                            'Tổng nhận: +${summary.totalCoinsEarned} xu',
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              color: Colors.orange,
                              fontSize: 15,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                  if (summary.progress > 0) ...[
                    const SizedBox(height: 8),
                    Text(
                      '📚 Đồng bộ ${summary.progress} tiến độ bài học.',
                      style: const TextStyle(fontSize: 13),
                    ),
                  ],
                  const SizedBox(height: 8),
                ],
              ),
            ),
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: const Text('OK!'),
            ),
          ],
        );
      },
    );
  }

  Future<void> _fetchUnread() async {
    try {
      final res = await ApiClient.instance.get('/notifications/unread-count');
      final count = (res.data is Map ? res.data['count'] : 0) as int;
      if (mounted) setState(() => _unreadCount = count);
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final version = context.watch<ProgressNotifier>().version;
    if (version != _lastVersion) {
      _lastVersion = version;
      _fetchUnread();
    }
    final nav = widget.navigationShell;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.auto_stories_rounded,
              color: Colors.white.withAlpha(220),
              size: 22,
            ),
            const SizedBox(width: 8),
            const Text(
              'Math IBook',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        actions: [
          Stack(
            children: [
              IconButton(
                icon: const Icon(
                  Icons.notifications_outlined,
                  color: Colors.white,
                ),
                onPressed: () => context.push('/student/notifications'),
              ),
              if (_unreadCount > 0)
                Positioned(
                  right: 6,
                  top: 6,
                  child: Container(
                    padding: const EdgeInsets.all(4),
                    decoration: const BoxDecoration(
                      color: Color(0xFFEF4444),
                      shape: BoxShape.circle,
                    ),
                    constraints: const BoxConstraints(
                      minWidth: 18,
                      minHeight: 18,
                    ),
                    child: Text(
                      '$_unreadCount',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                      ),
                      textAlign: TextAlign.center,
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
      body: nav,
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          border: Border(
            top: BorderSide(color: const Color(0xFFE2E8F0).withAlpha(80)),
          ),
        ),
        child: NavigationBar(
          selectedIndex: nav.currentIndex,
          onDestinationSelected: (index) {
            nav.goBranch(index, initialLocation: index == nav.currentIndex);
          },
          height: 68,
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.home_outlined),
              selectedIcon: Icon(Icons.home_rounded),
              label: 'Trang chủ',
            ),
            NavigationDestination(
              icon: Icon(Icons.book_outlined),
              selectedIcon: Icon(Icons.book_rounded),
              label: 'Chương học',
            ),
            NavigationDestination(
              icon: Icon(Icons.leaderboard_outlined),
              selectedIcon: Icon(Icons.leaderboard_rounded),
              label: 'Bảng xếp hạng',
            ),
            NavigationDestination(
              icon: Icon(Icons.person_outline_rounded),
              selectedIcon: Icon(Icons.person_rounded),
              label: 'Cá nhân',
            ),
          ],
        ),
      ),
    );
  }
}
