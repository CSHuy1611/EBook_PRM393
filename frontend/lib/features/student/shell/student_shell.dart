import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:math_ibook/core/network/api_client.dart';

class StudentShell extends StatefulWidget {
  final StatefulNavigationShell navigationShell;

  const StudentShell({super.key, required this.navigationShell});

  @override
  State<StudentShell> createState() => _StudentShellState();
}

class _StudentShellState extends State<StudentShell> {
  int _unreadCount = 0;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _fetchUnread();
    _timer = Timer.periodic(const Duration(seconds: 30), (_) => _fetchUnread());
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
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
    final nav = widget.navigationShell;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Math IBook'),
        actions: [
          Stack(
            children: [
              IconButton(
                icon: const Icon(Icons.notifications),
                onPressed: () => context.push('/student/notifications'),
              ),
              if (_unreadCount > 0)
                Positioned(
                  right: 6,
                  top: 6,
                  child: Container(
                    padding: const EdgeInsets.all(4),
                    decoration: const BoxDecoration(
                      color: Colors.red,
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
      bottomNavigationBar: NavigationBar(
        selectedIndex: nav.currentIndex,
        onDestinationSelected: (index) {
          nav.goBranch(index, initialLocation: true);
        },
        destinations: const [
          NavigationDestination(icon: Icon(Icons.home), label: 'Trang chủ'),
          NavigationDestination(icon: Icon(Icons.book), label: 'Bài học'),
          NavigationDestination(icon: Icon(Icons.dashboard), label: 'Bảng tin'),
          NavigationDestination(icon: Icon(Icons.person), label: 'Cá nhân'),
        ],
      ),
    );
  }
}
