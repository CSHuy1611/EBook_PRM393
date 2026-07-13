import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class UserHistoryScreen extends StatefulWidget {
  final String userId;
  const UserHistoryScreen({super.key, required this.userId});

  @override
  State<UserHistoryScreen> createState() => _UserHistoryScreenState();
}

class _UserHistoryScreenState extends State<UserHistoryScreen>
    with SingleTickerProviderStateMixin {
  UserHistoryDto? _history;
  bool _isLoading = true;
  String? _error;
  late TabController _tabCtrl;

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 3, vsync: this);
    _fetchHistory();
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  Future<void> _fetchHistory() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/admin/users/${widget.userId}/history');
      final data = response.data;
      Map<String, dynamic> map;
      if (data is Map<String, dynamic>) {
        map = data;
        if (map.containsKey('data') && map['data'] is Map<String, dynamic>) {
          map = map['data'] as Map<String, dynamic>;
        }
      } else {
        throw const FormatException('Invalid response format');
      }
      _history = UserHistoryDto.fromJson(map);
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lịch sử người dùng'),
        bottom: _isLoading || _error != null
            ? null
            : TabBar(
                controller: _tabCtrl,
                tabs: [
                  Tab(text: 'Bài quiz (${_history?.quizAttempts.length ?? 0})'),
                  Tab(text: 'Huy hiệu (${_history?.badges.length ?? 0})'),
                  Tab(text: 'Giao dịch (${_history?.coinTransactions.length ?? 0})'),
                ],
              ),
      ),
      body: _isLoading
          ? const AppLoadingWidget(message: 'Đang tải lịch sử...')
          : _error != null
              ? AppErrorWidget(message: _error!, onRetry: _fetchHistory)
              : TabBarView(
                  controller: _tabCtrl,
                  children: [
                    _buildQuizAttempts(),
                    _buildBadges(),
                    _buildCoinTransactions(),
                  ],
                ),
    );
  }

  Widget _buildQuizAttempts() {
    final attempts = _history?.quizAttempts ?? [];
    if (attempts.isEmpty) {
      return const Center(child: Text('Chưa có bài quiz nào'));
    }
    return RefreshIndicator(
      onRefresh: _fetchHistory,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: attempts.length,
        itemBuilder: (context, index) {
          final a = attempts[index];
          final scorePercent = a.totalQuestions > 0 ? (a.score / a.totalQuestions * 100) : 0.0;
          final minutes = a.durationSeconds ~/ 60;
          final seconds = a.durationSeconds % 60;
          return Card(
            margin: const EdgeInsets.only(bottom: 8),
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(a.lessonTitle.isNotEmpty ? a.lessonTitle : 'Bài học',
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                  if (a.chapterTitle.isNotEmpty)
                    Text('Chương: ${a.chapterTitle}',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      _buildStat('Điểm', '${a.score}/${a.totalQuestions}', Colors.blue),
                      const SizedBox(width: 16),
                      _buildStat('Tỷ lệ', '${scorePercent.toStringAsFixed(0)}%', Colors.green),
                      const SizedBox(width: 16),
                      _buildStat('Thời gian', '${minutes}p${seconds}s', Colors.orange),
                    ],
                  ),
                  if (a.createdAt.isNotEmpty) ...[
                    const SizedBox(height: 4),
                    Text(_formatDate(a.createdAt),
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
                  ],
                  if (scorePercent >= 80)
                    const Padding(
                      padding: EdgeInsets.only(top: 4),
                      child: Row(
                        children: [
                          Icon(Icons.emoji_events, size: 16, color: Colors.amber),
                          SizedBox(width: 4),
                          Text('Xuất sắc!', style: TextStyle(color: Colors.amber, fontWeight: FontWeight.w600, fontSize: 12)),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildBadges() {
    final badges = _history?.badges ?? [];
    if (badges.isEmpty) {
      return const Center(child: Text('Chưa có huy hiệu nào'));
    }
    return RefreshIndicator(
      onRefresh: _fetchHistory,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: badges.length,
        itemBuilder: (context, index) {
          final b = badges[index];
          return Card(
            margin: const EdgeInsets.only(bottom: 8),
            child: ListTile(
              leading: ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: b.iconUrl.isNotEmpty
                    ? Image.network(
                        b.iconUrl,
                        width: 40,
                        height: 40,
                        errorBuilder: (_, __, ___) => const Icon(Icons.emoji_events, color: Colors.amber),
                      )
                    : const Icon(Icons.emoji_events, color: Colors.amber, size: 40),
              ),
              title: Text(b.title, style: const TextStyle(fontWeight: FontWeight.w600)),
              subtitle: b.description.isNotEmpty ? Text(b.description, maxLines: 2) : null,
            ),
          );
        },
      ),
    );
  }

  Widget _buildCoinTransactions() {
    final transactions = _history?.coinTransactions ?? [];
    if (transactions.isEmpty) {
      return const Center(child: Text('Chưa có giao dịch coin nào'));
    }
    return RefreshIndicator(
      onRefresh: _fetchHistory,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: transactions.length,
        itemBuilder: (context, index) {
          final t = transactions[index];
          return Card(
            margin: const EdgeInsets.only(bottom: 8),
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor: t.amount > 0 ? Colors.green.withAlpha(25) : Colors.red.withAlpha(25),
                child: Icon(
                  t.amount > 0 ? Icons.add_circle : Icons.remove_circle,
                  color: t.amount > 0 ? Colors.green : Colors.red,
                ),
              ),
              title: Text(
                '${t.amount > 0 ? '+' : ''}${t.amount} coin',
                style: TextStyle(
                  fontWeight: FontWeight.w600,
                  color: t.amount > 0 ? Colors.green : Colors.red,
                ),
              ),
              subtitle: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(_getSourceLabel(t.sourceType)),
                  if (t.description.isNotEmpty) Text(t.description, maxLines: 1),
                  if (t.createdAt.isNotEmpty)
                    Text(_formatDate(t.createdAt),
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
                ],
              ),
              isThreeLine: true,
            ),
          );
        },
      ),
    );
  }

  Widget _buildStat(String label, String value, Color color) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
        Text(value, style: TextStyle(fontWeight: FontWeight.bold, color: color)),
      ],
    );
  }

  String _getSourceLabel(String type) {
    switch (type) {
      case 'quiz_completion':
        return 'Hoàn thành quiz';
      case 'daily_login':
        return 'Đăng nhập hàng ngày';
      case 'badge_reward':
        return 'Thưởng huy hiệu';
      case 'admin_adjustment':
        return 'Điều chỉnh bởi admin';
      default:
        return type;
    }
  }

  String _formatDate(String dateStr) {
    try {
      final dt = DateTime.parse(dateStr);
      return '${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')} ${dt.day}/${dt.month}/${dt.year}';
    } catch (_) {
      return dateStr;
    }
  }
}
