import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/dashboard_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';

class StudentHomeScreen extends StatefulWidget {
  const StudentHomeScreen({super.key});

  @override
  State<StudentHomeScreen> createState() => _StudentHomeScreenState();
}

class _StudentHomeScreenState extends State<StudentHomeScreen> {
  DashboardDto? _dash;
  bool _isLoading = true;
  int _lastVersion = -1;

  @override
  void initState() {
    super.initState();
    _fetchStats();
  }

  Future<void> _fetchStats() async {
    try {
      final res = await ApiClient.instance.get('/dashboard/me');
      setState(() {
        _dash = DashboardDto.fromJson(res.data as Map<String, dynamic>);
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final version = context.watch<ProgressNotifier>().version;
    if (version != _lastVersion) {
      _lastVersion = version;
      if (_dash != null && !_isLoading) _fetchStats();
    }
    return SingleChildScrollView(
      child: Column(
        children: [
          _buildHeader(context),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 0),
            child: Column(
              children: [
                if (_dash != null) ...[
                  const SizedBox(height: 20),
                  _buildStatsGrid(context),
                  const SizedBox(height: 20),
                  _buildContinueLearning(context),
                  const SizedBox(height: 20),
                  _buildQuickActions(context),
                  if (_dash!.badges.isNotEmpty) ...[
                    const SizedBox(height: 20),
                    _buildBadges(context),
                  ],
                  if (_dash!.recentActivities.isNotEmpty) ...[
                    const SizedBox(height: 20),
                    _buildRecentActivities(context),
                  ],
                ] else if (!_isLoading) ...[
                  const SizedBox(height: 20),
                  _buildQuickActions(context),
                ] else
                  const SizedBox(height: 40),
                const SizedBox(height: 24),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;
    final name = user?.name ?? 'Học sinh';
    final initial = name.isNotEmpty ? name[0].toUpperCase() : '?';
    final coins = _dash?.totalCoins ?? user?.coins ?? 0;
    final top = MediaQuery.of(context).padding.top;

    return Container(
      width: double.infinity,
      padding: EdgeInsets.fromLTRB(20, top + 20, 20, 24),
      color: Theme.of(context).colorScheme.primary,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 26,
                backgroundColor: Colors.white.withAlpha(30),
                child: Text(
                  initial,
                  style: const TextStyle(fontSize: 22, color: Colors.white, fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Xin chào', style: TextStyle(color: Colors.white70, fontSize: 13)),
                    Text(
                      name,
                      style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: Colors.white.withAlpha(25),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.monetization_on_rounded, color: Colors.amber.shade300, size: 18),
                    const SizedBox(width: 4),
                    Text(
                      '$coins',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (_dash != null) ...[
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(6),
                    child: LinearProgressIndicator(
                      value: _dash!.overallCompletionPercentage / 100,
                      minHeight: 6,
                      backgroundColor: Colors.white.withAlpha(30),
                      valueColor: AlwaysStoppedAnimation<Color>(Colors.amber.shade300),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Text(
                  '${_dash!.overallCompletionPercentage.toStringAsFixed(0)}%',
                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14),
                ),
              ],
            ),
            Text(
              'Tiến độ tổng thể (${_dash!.completedLessons}/${_dash!.totalLessons} bài học)',
              style: TextStyle(color: Colors.white.withAlpha(170), fontSize: 12),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildStatsGrid(BuildContext context) {
    final dash = _dash!;
    final totalChapters = dash.chapterProgress.length;

    return Row(
      children: [
        Expanded(
          child: _statCard(
            context,
            Icons.menu_book_rounded,
            'Chương học',
            '$totalChapters',
            const Color(0xFF3B82F6),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _statCard(
            context,
            Icons.score_rounded,
            'Điểm TB',
            dash.averageScore.toStringAsFixed(1),
            const Color(0xFF10B981),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _statCard(
            context,
            Icons.emoji_events_rounded,
            'Huy hiệu',
            '${dash.badges.length}',
            const Color(0xFFF59E0B),
          ),
        ),
      ],
    );
  }

  Widget _statCard(BuildContext context, IconData icon, String label, String value, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 8),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Column(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: color.withAlpha(20),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: color, size: 20),
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: color),
          ),
          Text(
            label,
            style: const TextStyle(fontSize: 11, color: Color(0xFF64748B)),
          ),
        ],
      ),
    );
  }

  Widget _buildContinueLearning(BuildContext context) {
    final chapters = _dash!.chapterProgress;
    if (chapters.isEmpty) return const SizedBox.shrink();

    // Only show the continue learning card if we have an active continueLearning lesson
    // OR if there is a chapter that has been started but is not fully complete (completionPercentage < 100).
    final hasActiveIncomplete = chapters.any((c) => c.completedLessons > 0 && c.completionPercentage < 100);
    final showBanner = _dash!.continueLearning != null || hasActiveIncomplete;
    if (!showBanner) return const SizedBox.shrink();

    final current = chapters.firstWhere(
      (c) => _dash!.continueLearning != null && c.chapterId == _dash!.continueLearning!.chapterId,
      orElse: () => chapters.firstWhere(
        (c) => c.completionPercentage < 100,
        orElse: () => chapters.last,
      ),
    );

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: Theme.of(context).brightness == Brightness.dark
              ? const Color(0xFF334155)
              : const Color(0xFFE2E8F0),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.primary.withAlpha(25),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(
                  Icons.play_circle_filled,
                  color: Theme.of(context).colorScheme.primary,
                  size: 22,
                ),
              ),
              const SizedBox(width: 12),
              Text(
                'Tiếp tục học',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(
            current.chapterTitle,
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w600,
              color: Theme.of(context).colorScheme.onSurface,
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(6),
                  child: LinearProgressIndicator(
                    value: current.completionPercentage / 100,
                    minHeight: 8,
                    backgroundColor: Theme.of(context).colorScheme.primary.withAlpha(20),
                    valueColor: AlwaysStoppedAnimation<Color>(
                      current.completionPercentage >= 100 ? const Color(0xFF10B981) : Theme.of(context).colorScheme.primary,
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Text(
                '${current.completionPercentage.toStringAsFixed(0)}%',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            '${current.completedLessons}/${current.totalLessons} bài học',
            style: TextStyle(fontSize: 13, color: Theme.of(context).colorScheme.onSurfaceVariant),
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: () {
                final lessonId = _dash?.continueLearning?.lessonId;
                if (lessonId != null && lessonId.isNotEmpty) {
                  context.go('/student/lessons/$lessonId');
                } else {
                  context.go('/student/chapters/${current.chapterId}');
                }
              },
              icon: const Icon(Icons.arrow_forward_rounded, size: 18),
              label: const Text('Học tiếp'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 14),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQuickActions(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 2, bottom: 14),
          child: Text(
            'Khám phá',
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: Theme.of(context).colorScheme.onSurface,
            ),
          ),
        ),
        Row(
          children: [
            Expanded(child: _actionCard(context, Icons.menu_book_rounded, 'Chương học', const Color(0xFF3B82F6), () => context.go('/student/chapters'))),
            const SizedBox(width: 12),
            Expanded(child: _actionCard(context, Icons.quiz_rounded, 'Kiểm tra', const Color(0xFF10B981), () => context.go('/student/chapters'))),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(child: _actionCard(context, Icons.leaderboard_rounded, 'Bảng xếp hạng', const Color(0xFFF59E0B), () => context.go('/student/leaderboard'))),
            const SizedBox(width: 12),
            Expanded(child: _actionCard(context, Icons.person_rounded, 'Cá nhân', const Color(0xFF8B5CF6), () => context.go('/student/profile'))),
          ],
        ),
      ],
    );
  }

  Widget _actionCard(BuildContext context, IconData icon, String label, Color color, VoidCallback onTap) {
    return Material(
      color: Theme.of(context).colorScheme.surface,
      borderRadius: BorderRadius.circular(14),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 20),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outlineVariant),
          ),
          child: Column(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: color.withAlpha(20),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(icon, color: color, size: 26),
              ),
              const SizedBox(height: 10),
              Text(
                label,
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: Theme.of(context).colorScheme.onSurface),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBadges(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Padding(
              padding: const EdgeInsets.only(left: 2, bottom: 14),
              child: Text(
                'Danh hiệu',
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).colorScheme.onSurface,
                ),
              ),
            ),
            TextButton(
              onPressed: () => context.push('/student/badges'),
              child: const Text('Xem tất cả'),
            ),
          ],
        ),
        SizedBox(
          height: 76,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: _dash!.badges.length,
            separatorBuilder: (_, __) => const SizedBox(width: 12),
            itemBuilder: (context, index) {
              final badge = _dash!.badges[index];
              return Container(
                width: 76,
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFF59E0B).withAlpha(50)),
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.military_tech_rounded, color: const Color(0xFFF59E0B), size: 28),
                    const SizedBox(height: 4),
                    Text(
                      badge.title,
                      style: TextStyle(fontSize: 9, fontWeight: FontWeight.w600, color: Theme.of(context).colorScheme.onSurfaceVariant),
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
      ],
    );
  }

  Widget _buildRecentActivities(BuildContext context) {
    final activities = _dash!.recentActivities.take(3).toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 2, bottom: 14),
          child: Text(
            'Hoạt động gần đây',
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: Theme.of(context).colorScheme.onSurface,
            ),
          ),
        ),
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: const Color(0xFFE2E8F0)),
          ),
          child: Column(
            children: activities.map((a) {
              IconData icon;
              Color color;
              switch (a.type) {
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
                      child: Text(a.description, style: TextStyle(fontSize: 14, color: Theme.of(context).colorScheme.onSurface)),
                    ),
                    Text(
                      a.timestamp.length >= 10 ? a.timestamp.substring(0, 10) : a.timestamp,
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
