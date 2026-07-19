import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/dashboard_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  DashboardDto? _dashboard;
  bool _isLoading = true;
  String? _error;
  int _lastVersion = -1;

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
    final version = context.watch<ProgressNotifier>().version;
    if (version != _lastVersion) {
      _lastVersion = version;
      if (_dashboard != null && !_isLoading) _fetchDashboard();
    }
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải thông tin...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchDashboard);
    if (_dashboard == null) return const Center(child: Text('Không có dữ liệu'));

    final dash = _dashboard!;

    return RefreshIndicator(
      onRefresh: _fetchDashboard,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildHeaderCard(context, dash),
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(child: _buildCoinsCard(context, dash)),
                const SizedBox(width: 12),
                Expanded(child: _buildScoreCard(context, dash)),
              ],
            ),
            const SizedBox(height: 20),
            _buildChapterProgress(context, dash),
            if (dash.badges.isNotEmpty) ...[
              const SizedBox(height: 20),
              _buildBadgesSection(context, dash),
            ],
            if (dash.recentActivities.isNotEmpty) ...[
              const SizedBox(height: 20),
              _buildRecentActivities(context, dash),
            ],
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildHeaderCard(BuildContext context, DashboardDto dash) {
    final theme = Theme.of(context);
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: theme.colorScheme.primary,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Tổng quan',
                      style: TextStyle(color: Colors.white.withAlpha(180), fontSize: 13),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'Tiến độ học tập',
                      style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
              ),
              SizedBox(
                width: 80,
                height: 80,
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    SizedBox(
                      width: 72,
                      height: 72,
                      child: CircularProgressIndicator(
                        value: dash.overallCompletionPercentage / 100,
                        strokeWidth: 7,
                        backgroundColor: Colors.white.withAlpha(30),
                        valueColor: AlwaysStoppedAnimation<Color>(Colors.amber.shade300),
                      ),
                    ),
                    Text(
                      '${dash.overallCompletionPercentage.toStringAsFixed(0)}%',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: dash.overallCompletionPercentage / 100,
              minHeight: 6,
              backgroundColor: Colors.white.withAlpha(30),
              valueColor: AlwaysStoppedAnimation<Color>(Colors.amber.shade300),
            ),
          ),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Đã học: ${dash.completedLessons}/${dash.totalLessons} bài', style: TextStyle(color: Colors.white.withAlpha(180), fontSize: 12)),
              Text(
                'Còn lại: ${dash.totalLessons - dash.completedLessons} bài',
                style: TextStyle(color: Colors.white.withAlpha(180), fontSize: 12),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildCoinsCard(BuildContext context, DashboardDto dash) {
    final coins = context.watch<AuthProvider>().currentUser?.coins ?? dash.totalCoins;
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: const Color(0xFFF59E0B).withAlpha(20),
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(Icons.monetization_on_rounded, color: Color(0xFFF59E0B), size: 22),
          ),
          const SizedBox(height: 14),
          Text(
            '$coins',
            style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Color(0xFF1E293B)),
          ),
          const Text(
            'Tổng xu',
            style: TextStyle(fontSize: 13, color: Color(0xFF64748B)),
          ),
        ],
      ),
    );
  }

  Widget _buildScoreCard(BuildContext context, DashboardDto dash) {
    final score = dash.averageScore;
    final color = score >= 7 ? const Color(0xFF10B981) : (score >= 4 ? const Color(0xFFF59E0B) : const Color(0xFFEF4444));
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: color.withAlpha(20),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(Icons.score_rounded, color: color, size: 22),
          ),
          const SizedBox(height: 14),
          Text(
            score.toStringAsFixed(1),
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: color),
          ),
          const Text(
            'Điểm trung bình',
            style: TextStyle(fontSize: 13, color: Color(0xFF64748B)),
          ),
        ],
      ),
    );
  }

  Widget _buildChapterProgress(BuildContext context, DashboardDto dash) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: const Color(0xFF3B82F6).withAlpha(20),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(Icons.leaderboard_rounded, color: Color(0xFF3B82F6), size: 20),
              ),
              const SizedBox(width: 10),
              const Text(
                'Tiến độ chương',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Color(0xFF1E293B)),
              ),
            ],
          ),
          const SizedBox(height: 16),
          ...dash.chapterProgress.map((cp) => Padding(
            padding: const EdgeInsets.only(bottom: 14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        cp.chapterTitle,
                        style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: Color(0xFF1E293B)),
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: cp.completionPercentage >= 100
                            ? const Color(0xFF10B981).withAlpha(20)
                            : const Color(0xFF3B82F6).withAlpha(20),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        '${cp.completedLessons}/${cp.totalLessons}',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: cp.completionPercentage >= 100
                              ? const Color(0xFF10B981)
                              : const Color(0xFF3B82F6),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                ClipRRect(
                  borderRadius: BorderRadius.circular(6),
                  child: LinearProgressIndicator(
                    value: cp.completionPercentage / 100,
                    minHeight: 8,
                    backgroundColor: const Color(0xFFE2E8F0),
                    valueColor: AlwaysStoppedAnimation<Color>(
                      cp.completionPercentage >= 100
                          ? const Color(0xFF10B981)
                          : const Color(0xFF3B82F6),
                    ),
                  ),
                ),
              ],
            ),
          )),
        ],
      ),
    );
  }

  Widget _buildBadgesSection(BuildContext context, DashboardDto dash) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 2, bottom: 14),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Danh hiệu',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: Color(0xFF1E293B)),
              ),
              TextButton(
                onPressed: () => context.push('/student/badges'),
                child: const Text('Xem tất cả'),
              ),
            ],
          ),
        ),
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: const Color(0xFFE2E8F0)),
          ),
          child: SizedBox(
            height: 76,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: dash.badges.length,
              separatorBuilder: (_, __) => const SizedBox(width: 12),
              itemBuilder: (context, index) {
                final badge = dash.badges[index];
                return Container(
                  width: 76,
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFFF7ED),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: const Color(0xFFFFEDD5)),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.military_tech_rounded, color: const Color(0xFFF59E0B), size: 28),
                      const SizedBox(height: 4),
                      Text(
                        badge.title,
                        style: const TextStyle(fontSize: 9, fontWeight: FontWeight.w600, color: Color(0xFFC2410C)),
                        textAlign: TextAlign.center,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRecentActivities(BuildContext context, DashboardDto dash) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 2, bottom: 14),
          child: Text(
            'Hoạt động gần đây',
            style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: Color(0xFF1E293B)),
          ),
        ),
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: const Color(0xFFE2E8F0)),
          ),
          child: Column(
            children: dash.recentActivities.take(3).map((activity) {
              IconData icon;
              Color color;
              switch (activity.type) {
                case 'quiz_attempt':
                  icon = Icons.quiz_rounded; color = const Color(0xFF10B981); break;
                case 'lesson':
                  icon = Icons.menu_book_rounded; color = const Color(0xFF3B82F6); break;
                case 'badge_earned':
                  icon = Icons.military_tech_rounded; color = const Color(0xFFF59E0B); break;
                case 'coin_transaction':
                  icon = Icons.monetization_on_rounded; color = const Color(0xFFF59E0B); break;
                default:
                  icon = Icons.circle; color = const Color(0xFF94A3B8); break;
              }
              return Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                child: Row(
                  children: [
                    Container(
                      width: 38,
                      height: 38,
                      decoration: BoxDecoration(
                        color: color.withAlpha(20),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Icon(icon, color: color, size: 20),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(activity.description, style: const TextStyle(fontSize: 14, color: Color(0xFF1E293B))),
                    ),
                    Text(
                      activity.timestamp.length >= 10 ? activity.timestamp.substring(0, 10) : activity.timestamp,
                      style: const TextStyle(fontSize: 12, color: Color(0xFF94A3B8)),
                    ),
                  ],
                ),
              );
            }).toList(),
          ),
        ),
      ],
    );
  }
}
