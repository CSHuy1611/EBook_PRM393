import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/storage/local_prefs_service.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  double _fontScale = 1.0;
  bool _isDarkMode = false;

  @override
  void initState() {
    super.initState();
    final prefs = LocalPrefsService();
    _fontScale = prefs.getFontScale();
    _isDarkMode = prefs.getThemeMode() == 'dark';
  }

  Future<void> _setFontScale(double scale) async {
    setState(() => _fontScale = scale);
    await LocalPrefsService().setFontScale(scale);
  }

  Future<void> _toggleTheme(bool isDark) async {
    setState(() => _isDarkMode = isDark);
    await LocalPrefsService().setThemeMode(isDark ? 'dark' : 'light');
  }

  void _confirmLogout() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc muốn đăng xuất không?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () {
              Navigator.pop(context);
              context.read<AuthProvider>().logout();
            },
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.currentUser;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          const SizedBox(height: 24),
          CircleAvatar(
            radius: 48,
            backgroundColor: Theme.of(context).colorScheme.primaryContainer,
            child: Text(
              user?.name.isNotEmpty == true ? user!.name[0].toUpperCase() : '?',
              style: TextStyle(
                fontSize: 36,
                fontWeight: FontWeight.bold,
                color: Theme.of(context).colorScheme.onPrimaryContainer,
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            user?.name ?? 'Người dùng',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          Text(
            user?.email ?? '',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.grey),
          ),
          const SizedBox(height: 4),
          Chip(
            label: Text(user?.role ?? 'Student'),
            avatar: const Icon(Icons.school, size: 18),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: Colors.amber.withAlpha(30),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(Icons.monetization_on, color: Colors.amber, size: 28),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Xu của bạn', style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.grey)),
                        Text(
                          '${user?.coins ?? 0}',
                          style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Cài đặt hiển thị', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      const Icon(Icons.text_fields),
                      const SizedBox(width: 12),
                      const Text('Cỡ chữ'),
                      const Spacer(),
                      SizedBox(
                        width: 200,
                        child: Slider(
                          value: _fontScale,
                          min: 0.6,
                          max: 2.0,
                          divisions: 14,
                          label: '${(_fontScale * 100).round()}%',
                          onChanged: _setFontScale,
                        ),
                      ),
                    ],
                  ),
                  const Divider(),
                  SwitchListTile(
                    title: const Row(
                      children: [
                        Icon(Icons.dark_mode),
                        SizedBox(width: 12),
                        Text('Chế độ tối'),
                      ],
                    ),
                    value: _isDarkMode,
                    onChanged: _toggleTheme,
                    contentPadding: EdgeInsets.zero,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: _confirmLogout,
              icon: const Icon(Icons.logout, color: Colors.red),
              label: const Text('Đăng xuất', style: TextStyle(color: Colors.red)),
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                side: const BorderSide(color: Colors.red),
              ),
            ),
          ),
          const SizedBox(height: 32),
        ],
      ),
    );
  }
}
