import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/storage/local_prefs_service.dart';

class AdminSettingsScreen extends StatelessWidget {
  const AdminSettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Consumer<LocalPrefsService>(
        builder: (context, prefs, _) {
          final isDark = prefs.getThemeMode() == 'dark';
          final fontScale = prefs.getFontScale();
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text('Cài đặt hệ thống', style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 16),
              Card(
                child: SwitchListTile(
                  title: const Text('Chế độ tối (Dark Mode)'),
                  subtitle: const Text('Giao diện nền đen'),
                  value: isDark,
                  onChanged: (value) {
                    prefs.setThemeMode(value ? 'dark' : 'light');
                  },
                ),
              ),
              const SizedBox(height: 16),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Kích thước chữ: ${fontScale.toStringAsFixed(1)}',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      Slider(
                        value: fontScale,
                        min: 0.8,
                        max: 1.5,
                        divisions: 7,
                        label: fontScale.toStringAsFixed(1),
                        onChanged: (value) {
                          prefs.setFontScale(value);
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
