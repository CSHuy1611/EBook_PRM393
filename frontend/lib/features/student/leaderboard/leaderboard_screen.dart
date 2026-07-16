import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/student_feature_api.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';

class LeaderboardScreen extends StatefulWidget {
  const LeaderboardScreen({super.key});
  @override
  State<LeaderboardScreen> createState() => _LeaderboardScreenState();
}

class _LeaderboardScreenState extends State<LeaderboardScreen> {
  LeaderboardModel? _data;
  String? _error;
  bool _loading = true;

  @override
  void initState() { super.initState(); _load(); }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final data = await StudentFeatureApi.instance.getLeaderboard();
      if (mounted) setState(() { _data = data; _loading = false; });
    } catch (error) {
      if (mounted) setState(() { _error = error.toString(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Scaffold(body: AppLoadingWidget(message: 'Đang tải bảng xếp hạng...'));
    if (_error != null) return Scaffold(body: AppErrorWidget(message: _error!, onRetry: _load));
    final data = _data!;
    final outsideTop100 = data.currentUser != null && data.currentUser!.rank > 100;
    return Scaffold(
      appBar: AppBar(title: const Text('Bảng xếp hạng')),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text('Cập nhật lúc ${DateFormat('HH:mm, dd/MM/yyyy').format(data.updatedAt.toLocal())}',
              style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 12),
            ...data.top100.map((entry) => _LeaderboardTile(entry: entry)),
            if (outsideTop100) ...[
              const Padding(padding: EdgeInsets.only(top: 20, bottom: 8), child: Text('Thứ hạng của bạn', style: TextStyle(fontWeight: FontWeight.bold))),
              _LeaderboardTile(entry: data.currentUser!, separate: true),
            ],
          ],
        ),
      ),
    );
  }
}

class _LeaderboardTile extends StatelessWidget {
  final LeaderboardEntryModel entry;
  final bool separate;
  const _LeaderboardTile({required this.entry, this.separate = false});

  Color? get _rankColor => switch (entry.rank) { 1 => Colors.amber.shade700, 2 => Colors.blueGrey, 3 => Colors.brown, _ => null };

  @override
  Widget build(BuildContext context) => Card(
    color: entry.isCurrentUser ? Theme.of(context).colorScheme.primaryContainer : null,
    child: ListTile(
      leading: SizedBox(width: 34, child: Center(child: Text('#${entry.rank}', style: TextStyle(fontWeight: FontWeight.bold, color: _rankColor)))),
      title: Text(entry.name, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text('${NumberFormat.decimalPattern('vi').format(entry.coins)} xu • ${entry.badgeCount} huy hiệu'),
      trailing: entry.isCurrentUser ? const Icon(Icons.person_pin, color: Colors.deepPurple) : null,
    ),
  );
}
