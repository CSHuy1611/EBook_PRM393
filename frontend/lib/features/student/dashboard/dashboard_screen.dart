import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/dashboard_model.dart';
import 'package:math_ibook/core/models/quiz_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  DashboardDto? _dashboard;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchDashboard();
  }

  Future<void> _fetchDashboard() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/dashboard/me');
      final data = response.data as Map<String, dynamic>;
      _dashboard = DashboardDto.fromJson(data);
      setState(() => _isLoading = false);
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải thông tin...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchDashboard);
    if (_dashboard == null) return const Center(child: Text('Không có dữ liệu'));

    final dash = _dashboard!;

    return RefreshIndicator(
      onRefresh: _fetchDashboard,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildOverallCard(dash),
            const SizedBox(height: 16),
            _buildCoinsCard(dash),
            const SizedBox(height: 16),
            _buildAverageScoreCard(dash),
            const SizedBox(height: 16),
            _buildChapterProgress(dash),
            const SizedBox(height: 16),
            if (dash.badges.isNotEmpty) ...[
              _buildBadgesSection(dash),
              const SizedBox(height: 16),
            ],
            if (dash.recentActivities.isNotEmpty) ...[
              _buildRecentActivities(dash),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildOverallCard(DashboardDto dash) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Tổng quan', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: Row(
                children: [
                  Expanded(
                    child: PieChart(
                      PieChartData(
                        sections: [
                          PieChartSectionData(
                            value: dash.overallCompletionPercentage,
                            color: Colors.deepPurple,
                            radius: 50,
                            title: '${dash.overallCompletionPercentage.toStringAsFixed(0)}%',
                            titleStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16),
                          ),
                          PieChartSectionData(
                            value: 100 - dash.overallCompletionPercentage,
                            color: Colors.grey.withAlpha(60),
                            radius: 50,
                            title: '',
                          ),
                        ],
                        sectionsSpace: 2,
                        centerSpaceRadius: 40,
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _legendItem(Colors.deepPurple, 'Hoàn thành'),
                      const SizedBox(height: 8),
                      _legendItem(Colors.grey.withAlpha(60), 'Còn lại'),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _legendItem(Color color, String label) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(width: 12, height: 12, decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(3))),
        const SizedBox(width: 8),
        Text(label, style: const TextStyle(fontSize: 13)),
      ],
    );
  }

  Widget _buildCoinsCard(DashboardDto dash) {
    final coins = context.watch<AuthProvider>().currentUser?.coins ?? dash.totalCoins;
    return Card(
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
                  Text('Tổng xu', style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.grey)),
                  Text(
                    '$coins',
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAverageScoreCard(DashboardDto dash) {
    final color = dash.averageScore >= 50 ? Colors.green : Colors.orange;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: color.withAlpha(30),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(Icons.score, color: color, size: 28),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Điểm trung bình', style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.grey)),
                  Text(
                    '${dash.averageScore.toStringAsFixed(1)}',
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold, color: color),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildChapterProgress(DashboardDto dash) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Tiến độ chương', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            ...dash.chapterProgress.map((cp) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(child: Text(cp.chapterTitle, style: const TextStyle(fontSize: 14))),
                      Text(
                        '${cp.completedLessons}/${cp.totalLessons}',
                        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(4),
                    child: LinearProgressIndicator(
                      value: cp.completionPercentage / 100,
                      minHeight: 8,
                      backgroundColor: Colors.grey.withAlpha(40),
                    ),
                  ),
                ],
              ),
            )),
          ],
        ),
      ),
    );
  }

  Widget _buildBadgesSection(DashboardDto dash) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Danh hiệu', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                TextButton(
                  onPressed: () => context.push('/student/badges'),
                  child: const Text('Xem tất cả'),
                ),
              ],
            ),
            const SizedBox(height: 8),
            SizedBox(
              height: 80,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: dash.badges.length,
                separatorBuilder: (_, __) => const SizedBox(width: 12),
                itemBuilder: (context, index) {
                  final badge = dash.badges[index];
                  return Column(
                    children: [
                      Icon(Icons.military_tech, size: 40, color: Colors.amber.shade700),
                      const SizedBox(height: 4),
                      Text(badge.title, style: const TextStyle(fontSize: 11)),
                    ],
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRecentActivities(DashboardDto dash) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hoạt động gần đây', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            ...dash.recentActivities.map((activity) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(
                children: [
                  Icon(
                    _activityIcon(activity.type),
                    size: 20,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(activity.description, style: const TextStyle(fontSize: 14)),
                        Text(
                          activity.timestamp.length >= 10 ? activity.timestamp.substring(0, 10) : activity.timestamp,
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            )),
          ],
        ),
      ),
    );
  }

  IconData _activityIcon(String type) {
    switch (type) {
      case 'quiz_attempt':
        return Icons.quiz;
      case 'lesson':
        return Icons.menu_book;
      case 'badge_earned':
        return Icons.military_tech;
      case 'coin_transaction':
        return Icons.monetization_on;
      default:
        return Icons.circle;
    }
  }
}
