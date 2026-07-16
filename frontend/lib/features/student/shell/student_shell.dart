import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
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
  int _unreadCount = 0;
  int _lastVersion = -1;
  Timer? _timer;
  StreamSubscription<List<ConnectivityResult>>? _connectivitySubscription;

  @override
  void initState() {
    super.initState();
    _fetchUnread();
    _timer = Timer.periodic(const Duration(seconds: 30), (_) => _fetchUnread());
    _syncPending();
    _connectivitySubscription = Connectivity().onConnectivityChanged.listen((result) {
      if (result.any((item) => item != ConnectivityResult.none)) _syncPending();
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    _connectivitySubscription?.cancel();
    super.dispose();
  }

  Future<void> _syncPending() async {
    final userId = context.read<AuthProvider>().currentUser?.id;
    if (userId == null || userId.isEmpty) return;
    try { await OfflineSyncService.instance.sync(userId); } catch (_) {}
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
          children: [
            Icon(Icons.auto_stories_rounded, color: Colors.white.withAlpha(220), size: 22),
            const SizedBox(width: 8),
            const Text(
              'Math IBook',
              style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        actions: [
          Stack(
            children: [
              IconButton(
                icon: const Icon(Icons.notifications_outlined, color: Colors.white),
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
                    constraints: const BoxConstraints(minWidth: 18, minHeight: 18),
                    child: Text(
                      '$_unreadCount',
                      style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold),
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
          border: Border(top: BorderSide(color: const Color(0xFFE2E8F0).withAlpha(80))),
        ),
        child: NavigationBar(
          selectedIndex: nav.currentIndex,
          onDestinationSelected: (index) {
            nav.goBranch(index, initialLocation: index == nav.currentIndex);
          },
          height: 68,
          destinations: const [
            NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home_rounded), label: 'Trang chủ'),
            NavigationDestination(icon: Icon(Icons.book_outlined), selectedIcon: Icon(Icons.book_rounded), label: 'Chương học'),
            NavigationDestination(icon: Icon(Icons.leaderboard_outlined), selectedIcon: Icon(Icons.leaderboard_rounded), label: 'Bảng xếp hạng'),
            NavigationDestination(icon: Icon(Icons.person_outline_rounded), selectedIcon: Icon(Icons.person_rounded), label: 'Cá nhân'),
          ],
        ),
      ),
    );
  }
}
