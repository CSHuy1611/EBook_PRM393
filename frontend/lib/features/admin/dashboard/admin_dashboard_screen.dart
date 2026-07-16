import 'dart:async';
import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminDashboardScreen extends StatefulWidget {
  const AdminDashboardScreen({super.key});

  @override
  State<AdminDashboardScreen> createState() => _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends State<AdminDashboardScreen> {
  ReportOverviewDto? _report;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchData();
  }

  Future<void> _fetchData() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/admin/reports/overview');
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
      _report = ReportOverviewDto.fromJson(map);
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _onRefresh() => _fetchData();

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải dữ liệu...');
    if (_error != null) {
      return AppErrorWidget(message: _error!, onRetry: _onRefresh);
    }
    final report = _report!;
    return RefreshIndicator(
      onRefresh: _onRefresh,
      child: LayoutBuilder(
        builder: (context, constraints) {
          final isWide = constraints.maxWidth > 800;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _buildStatCards(report, isWide),
              const SizedBox(height: 24),
              if (isWide)
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      flex: 2,
                      child: Column(
                        children: [
                          _buildDailyActivityChart(report.dailyActivities),
                          const SizedBox(height: 24),
                          _buildUserGrowthChart(report.dailyActivities),
                          const SizedBox(height: 24),
                          _buildChapterCompletionChart(report.chapterReports),
                        ],
                      ),
                    ),
                    const SizedBox(width: 24),
                    Expanded(
                      flex: 1,
                      child: Column(
                        children: [
                          _buildTopStudents(report.topStudents),
                          const SizedBox(height: 24),
                          _buildMostFailedQuestions(report.mostFailedQuestions),
                        ],
                      ),
                    ),
                  ],
                )
              else
                Column(
                  children: [
                    _buildDailyActivityChart(report.dailyActivities),
                    const SizedBox(height: 24),
                    _buildUserGrowthChart(report.dailyActivities),
                    const SizedBox(height: 24),
                    _buildChapterCompletionChart(report.chapterReports),
                    const SizedBox(height: 24),
                    _buildTopStudents(report.topStudents),
                    const SizedBox(height: 24),
                    _buildMostFailedQuestions(report.mostFailedQuestions),
                  ],
                ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildStatCards(ReportOverviewDto report, bool isWide) {
    final cards = [
      _StatCard(
        icon: Icons.people,
        label: 'Tổng người dùng',
        value: '${report.totalUsers}',
        color: Colors.blue,
      ),
      _StatCard(
        icon: Icons.quiz,
        label: 'Bài quiz đã làm',
        value: '${report.totalQuizAttempts}',
        color: Colors.green,
      ),
      _StatCard(
        icon: Icons.score,
        label: 'Điểm trung bình',
        value: '${report.overallAverageScore.toStringAsFixed(1)}%',
        color: Colors.orange,
      ),
      _StatCard(
        icon: Icons.monetization_on,
        label: 'Coin đã thưởng',
        value: '${report.totalCoinsAwarded}',
        color: Colors.purple,
      ),
      _StatCard(
        icon: Icons.workspace_premium,
        label: 'Huy hiệu đã thưởng',
        value: '${report.totalBadgesAwarded}',
        color: Colors.red,
      ),
      _StatCard(
        icon: Icons.check_circle_outline,
        label: 'Tiến độ học tập',
        value: '${report.chapterReports.isEmpty ? 0 : (report.chapterReports.map((c) => c.completionRate).reduce((a, b) => a + b) / report.chapterReports.length).toStringAsFixed(1)}%',
        color: Colors.teal,
      ),
    ];
    if (isWide) {
      return Row(children: cards.map((c) => Expanded(child: c)).toList());
    }
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: cards.map((c) => SizedBox(width: MediaQuery.of(context).size.width / 2 - 24, child: c)).toList(),
    );
  }

  Widget _buildDailyActivityChart(List<DailyActivityDto> activities) {
    final display = activities.length > 14 ? activities.sublist(activities.length - 14) : activities;
    final maxVal = display.fold<int>(0, (max, a) => a.quizCount > max ? a.quizCount : max);
    final bars = display.asMap().entries.map((e) {
      return BarChartGroupData(
        x: e.key,
        barRods: [
          BarChartRodData(
            toY: e.value.quizCount.toDouble(),
            color: Colors.blue,
            width: isWideScreen() ? 16 : 12,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
          ),
        ],
      );
    }).toList();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Lượt làm bài (14 ngày qua)', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: BarChart(
                BarChartData(
                  alignment: BarChartAlignment.spaceAround,
                  maxY: (maxVal * 1.3).clamp(5, double.infinity),
                  barTouchData: BarTouchData(
                    touchTooltipData: BarTouchTooltipData(
                      getTooltipItem: (group, groupIndex, rod, rodIndex) {
                        final d = display[group.x];
                        return BarTooltipItem(
                          '${d.date}\nLượt làm: ${d.quizCount}',
                          const TextStyle(color: Colors.white),
                        );
                      },
                    ),
                  ),
                  titlesData: FlTitlesData(
                    show: true,
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        getTitlesWidget: (value, meta) {
                          final idx = value.toInt();
                          if (idx < 0 || idx >= display.length) return const SizedBox();
                          final date = display[idx].date;
                          final parts = date.split('-');
                          final label = parts.length >= 3 ? '${parts[2]}/${parts[1]}' : date;
                          return Padding(
                            padding: const EdgeInsets.only(top: 4),
                            child: Text(label, style: const TextStyle(fontSize: 9)),
                          );
                        },
                        reservedSize: 28,
                      ),
                    ),
                    leftTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 36,
                        getTitlesWidget: (value, meta) {
                          return Text('${value.toInt()}', style: const TextStyle(fontSize: 10));
                        },
                      ),
                    ),
                    topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                    rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  ),
                  borderData: FlBorderData(show: false),
                  gridData: FlGridData(
                    show: true,
                    drawVerticalLine: false,
                    horizontalInterval: maxVal > 10 ? (maxVal / 5).ceilToDouble() : 1,
                  ),
                  barGroups: bars,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildUserGrowthChart(List<DailyActivityDto> activities) {
    final display = activities.length > 14 ? activities.sublist(activities.length - 14) : activities;
    final maxVal = display.fold<int>(0, (m, a) => a.newUsers > m ? a.newUsers : m);
    if (maxVal == 0) {
      return Card(child: Padding(
        padding: const EdgeInsets.all(16),
        child: Text('Chưa có dữ liệu người dùng mới', style: Theme.of(context).textTheme.bodyMedium),
      ));
    }
    final bars = display.asMap().entries.map((e) {
      return BarChartGroupData(
        x: e.key,
        barRods: [
          BarChartRodData(
            toY: e.value.newUsers.toDouble(),
            color: Colors.indigo,
            width: isWideScreen() ? 16 : 12,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
          ),
        ],
      );
    }).toList();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Người dùng đăng ký mới (14 ngày qua)', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: BarChart(
                BarChartData(
                  alignment: BarChartAlignment.spaceAround,
                  maxY: (maxVal * 1.4).clamp(3, double.infinity),
                  barTouchData: BarTouchData(
                    touchTooltipData: BarTouchTooltipData(
                      getTooltipItem: (group, _, rod, __) {
                        final d = display[group.x];
                        return BarTooltipItem('${d.date}\nMới: ${d.newUsers}', const TextStyle(color: Colors.white));
                      },
                    ),
                  ),
                  titlesData: FlTitlesData(
                    show: true,
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(showTitles: true, reservedSize: 28,
                        getTitlesWidget: (value, meta) {
                          final idx = value.toInt();
                          if (idx < 0 || idx >= display.length) return const SizedBox();
                          final date = display[idx].date;
                          final parts = date.split('-');
                          final label = parts.length >= 3 ? '${parts[2]}/${parts[1]}' : date;
                          return Padding(padding: const EdgeInsets.only(top: 4), child: Text(label, style: const TextStyle(fontSize: 9)));
                        },
                      ),
                    ),
                    leftTitles: AxisTitles(
                      sideTitles: SideTitles(showTitles: true, reservedSize: 36,
                        getTitlesWidget: (v, _) => Text('${v.toInt()}', style: const TextStyle(fontSize: 10)),
                      ),
                    ),
                    topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                    rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  ),
                  borderData: FlBorderData(show: false),
                  gridData: FlGridData(show: true, drawVerticalLine: false),
                  barGroups: bars,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildChapterCompletionChart(List<ChapterReportDto> chapters) {
    if (chapters.isEmpty) {
      return Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Text('Chưa có dữ liệu chương', style: Theme.of(context).textTheme.bodyMedium),
        ),
      );
    }
    final maxVal = chapters.fold<double>(0, (m, c) => c.completionRate > m ? c.completionRate : m);
    final isWide = isWideScreen();
    final bars = chapters.asMap().entries.map((e) {
      return BarChartGroupData(
        x: e.key,
        barRods: [
          BarChartRodData(
            toY: e.value.completionRate,
            color: Colors.teal,
            width: isWide ? 20 : 16,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
          ),
        ],
      );
    }).toList();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Tỷ lệ hoàn thành theo chương', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: BarChart(
                BarChartData(
                  alignment: BarChartAlignment.spaceAround,
                  maxY: (maxVal * 1.3).clamp(10, 100.0),
                  barTouchData: BarTouchData(
                    touchTooltipData: BarTouchTooltipData(
                      getTooltipItem: (group, groupIndex, rod, rodIndex) {
                        final c = chapters[group.x];
                        return BarTooltipItem(
                          '${c.chapterTitle}\n${c.completionRate.toStringAsFixed(1)}%',
                          const TextStyle(color: Colors.white),
                        );
                      },
                    ),
                  ),
                  titlesData: FlTitlesData(
                    show: true,
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        getTitlesWidget: (value, meta) {
                          final idx = value.toInt();
                          if (idx < 0 || idx >= chapters.length) return const SizedBox();
                          final title = chapters[idx].chapterTitle;
                          return Padding(
                            padding: const EdgeInsets.only(top: 4),
                            child: Text(
                              title.length > 10 ? '${title.substring(0, 10)}...' : title,
                              style: const TextStyle(fontSize: 9),
                            ),
                          );
                        },
                        reservedSize: 28,
                      ),
                    ),
                    leftTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 36,
                        getTitlesWidget: (value, meta) {
                          return Text('${value.toInt()}%', style: const TextStyle(fontSize: 10));
                        },
                      ),
                    ),
                    topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                    rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  ),
                  borderData: FlBorderData(show: false),
                  gridData: FlGridData(
                    show: true,
                    drawVerticalLine: false,
                    horizontalInterval: 20,
                  ),
                  barGroups: bars,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTopStudents(List<TopStudentDto> students) {
    if (students.isEmpty) {
      return Card(child: Padding(
        padding: const EdgeInsets.all(16),
        child: Text('Chưa có dữ liệu học sinh', style: Theme.of(context).textTheme.bodyMedium),
      ));
    }
    return Card(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Text('Học sinh xuất sắc', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          ),
          const Divider(height: 1),
          Column(
            children: students.take(5).toList().asMap().entries.map((e) {
              final idx = e.key + 1;
              final s = e.value;
              return ListTile(
                leading: CircleAvatar(
                  backgroundColor: idx <= 3 ? Colors.orange.withAlpha(50) : Colors.blue.withAlpha(25),
                  child: Text('#$idx', style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: idx <= 3 ? Colors.deepOrange : Colors.blue,
                  )),
                ),
                title: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                trailing: Wrap(
                  spacing: 8,
                  children: [
                    Chip(
                      label: Text('${s.coins}', style: const TextStyle(color: Colors.amber, fontSize: 12)),
                      avatar: const Icon(Icons.monetization_on, color: Colors.amber, size: 16),
                      backgroundColor: Colors.amber.withAlpha(20),
                      side: BorderSide.none,
                      padding: EdgeInsets.zero,
                    ),
                    Chip(
                      label: Text('${s.badgeCount}', style: const TextStyle(color: Colors.purple, fontSize: 12)),
                      avatar: const Icon(Icons.emoji_events, color: Colors.purple, size: 16),
                      backgroundColor: Colors.purple.withAlpha(20),
                      side: BorderSide.none,
                      padding: EdgeInsets.zero,
                    ),
                  ],
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }

  Widget _buildMostFailedQuestions(List<FailedQuestionDto> questions) {
    if (questions.isEmpty) {
      return Card(child: Padding(
        padding: const EdgeInsets.all(16),
        child: Text('Chưa có dữ liệu câu hỏi sai', style: Theme.of(context).textTheme.bodyMedium),
      ));
    }
    return Card(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Text('Câu hỏi làm sai nhiều', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          ),
          const Divider(height: 1),
          Column(
            children: questions.take(5).map((q) {
              return ListTile(
                leading: const Icon(Icons.error_outline, color: Colors.red),
                title: Text(q.content, maxLines: 2, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 13)),
                subtitle: Text('Sai: ${q.failedAttempts}/${q.totalAttempts} lần (${q.failureRate}%)', style: const TextStyle(fontSize: 12)),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }

  bool isWideScreen() => MediaQuery.of(context).size.width > 700;
}

class _StatCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _StatCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: color.withAlpha(25),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(icon, color: color, size: 24),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: Theme.of(context).textTheme.bodySmall?.copyWith(fontSize: 11), maxLines: 1, overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 2),
                  Text(value,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
