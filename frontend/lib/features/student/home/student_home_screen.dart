import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';

class StudentHomeScreen extends StatelessWidget {
  const StudentHomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;
    return Scaffold(
      appBar: AppBar(title: const Text('Trang chủ')),
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
                    CircleAvatar(
                      radius: 30,
                      backgroundColor: Theme.of(context).primaryColor,
                      child: Text(
                        user?.name.isNotEmpty == true ? user!.name[0].toUpperCase() : '?',
                        style: const TextStyle(fontSize: 24, color: Colors.white),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(user?.name ?? 'Học sinh', style: Theme.of(context).textTheme.titleLarge),
                        Text(user?.email ?? '', style: Theme.of(context).textTheme.bodySmall),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            Text('Chào mừng đến với Math IBook!', style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 8),
            Text('Ứng dụng sách giáo khoa Toán 8 tương tác', style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 24),
            Expanded(
              child: GridView.count(
                crossAxisCount: 2,
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                children: [
                  _buildMenuItem(context, Icons.book, 'Bài học', Colors.blue, () => context.go('/student/chapters')),
                  _buildMenuItem(context, Icons.quiz, 'Kiểm tra', Colors.green, () => context.go('/student/chapters')),
                  _buildMenuItem(context, Icons.dashboard, 'Bảng tin', Colors.orange, () => context.go('/student/dashboard')),
                  _buildMenuItem(context, Icons.emoji_events, 'Huy hiệu', Colors.purple, () => context.go('/student/badges')),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMenuItem(BuildContext context, IconData icon, String label, Color color, VoidCallback onTap) {
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 48, color: color),
            const SizedBox(height: 8),
            Text(label, style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500, color: color)),
          ],
        ),
      ),
    );
  }
}
