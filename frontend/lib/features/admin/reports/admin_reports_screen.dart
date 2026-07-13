import 'dart:async';
import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminReportsScreen extends StatefulWidget {
  const AdminReportsScreen({super.key});

  @override
  State<AdminReportsScreen> createState() => _AdminReportsScreenState();
}

class _AdminReportsScreenState extends State<AdminReportsScreen> {
  ReportOverviewDto? _report;
  bool _isLoading = true;
  String? _error;
  String _dateFilter = '7';

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
      final response = await ApiClient.instance.get('/admin/reports/overview', queryParameters: {
        'days': _dateFilter,
      });
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
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải báo cáo...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchData);
    final report = _report!;
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _fetchData,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth > 800;
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _buildDateFilter(),
                const SizedBox(height: 16),
                _buildSummaryCards(report, isWide),
                const SizedBox(height: 24),
                _buildSectionTitle('Bài quiz hàng ngày'),
                const SizedBox(height: 8),
                _buildDailyQuizChart(report.dailyActivities),
                const SizedBox(height: 24),
                _buildSectionTitle('Điểm trung bình theo chương'),
                const SizedBox(height: 8),
                _buildChapterScoreChart(report.chapterReports),
                const SizedBox(height: 24),
                _buildSectionTitle('Người dùng mới'),
                const SizedBox(height: 8),
                _buildUserGrowthChart(report.dailyActivities),
                const SizedBox(height: 24),
                _buildSectionTitle('Bài học có điểm thấp'),
                const SizedBox(height: 8),
                _buildLowScoreLessons(report.chapterReports),
              ],
            );
          },
        ),
      ),
    );
  }

  Widget _buildDateFilter() {
    return Row(
      children: [
        const Text('Khoảng thời gian: ', style: TextStyle(fontWeight: FontWeight.w500)),
        const SizedBox(width: 8),
        SegmentedButton<String>(
          segments: const [
            ButtonSegment(value: '7', label: Text('7 ngày')),
            ButtonSegment(value: '14', label: Text('14 ngày')),
            ButtonSegment(value: '30', label: Text('30 ngày')),
            ButtonSegment(value: '90', label: Text('90 ngày')),
          ],
          selected: {_dateFilter},
          onSelectionChanged: (v) {
            setState(() => _dateFilter = v.first);
            _fetchData();
          },
        ),
      ],
    );
  }

  Widget _buildSectionTitle(String title) {
    return Text(title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold));
  }

  Widget _buildSummaryCards(ReportOverviewDto report, bool isWide) {
    final cards = [
      _SummaryCard(icon: Icons.people, label: 'Tổng người dùng', value: '${report.totalUsers}', color: Colors.blue),
      _SummaryCard(icon: Icons.quiz, label: 'Bài quiz đã làm', value: '${report.totalQuizAttempts}', color: Colors.green),
      _SummaryCard(icon: Icons.score, label: 'Điểm TB', value: '${report.overallAverageScore.toStringAsFixed(1)}%', color: Colors.orange),
      _SummaryCard(icon: Icons.monetization_on, label: 'Coin đã thưởng', value: '${report.totalCoinsAwarded}', color: Colors.purple),
      _SummaryCard(icon: Icons.emoji_events, label: 'Huy hiệu đã thưởng', value: '${report.totalBadgesAwarded}', color: Colors.amber),
    ];
    if (isWide) {
      return Row(children: cards.map((c) => Expanded(child: c)).toList());
    }
    return Wrap(children: cards.map((c) => SizedBox(width: MediaQuery.of(context).size.width / 2 - 24, child: c)).toList());
  }

  Widget _buildDailyQuizChart(List<DailyActivityDto> activities) {
    final display = _getFilteredActivities(activities);
    final maxVal = display.fold<int>(0, (m, a) => a.quizCount > m ? a.quizCount : m);
    final bars = display.asMap().entries.map((e) {
      return BarChartGroupData(
        x: e.key,
        barRods: [
          BarChartRodData(
            toY: e.value.quizCount.toDouble(),
            color: Colors.blue,
            width: display.length > 20 ? 8 : 16,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
          ),
        ],
      );
    }).toList();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: SizedBox(
          height: 250,
          child: BarChart(
            BarChartData(
              alignment: BarChartAlignment.spaceAround,
              maxY: (maxVal * 1.3).clamp(5, double.infinity),
              barTouchData: BarTouchData(
                touchTooltipData: BarTouchTooltipData(
                  getTooltipItem: (group, _, rod, __) {
                    final d = display[group.x];
                    return BarTooltipItem('${d.date}\nQuiz: ${d.quizCount}', const TextStyle(color: Colors.white));
                  },
                ),
              ),
              titlesData: FlTitlesData(
                show: true,
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 28,
                    interval: display.length > 20 ? 2 : 1,
                    getTitlesWidget: (value, meta) {
                      final idx = value.toInt();
                      if (idx < 0 || idx >= display.length) return const SizedBox();
                      final date = display[idx].date;
                      final parts = date.split('-');
                      final label = parts.length >= 3 ? '${parts[2]}/${parts[1]}' : date;
                      return Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Text(label, style: const TextStyle(fontSize: 8)),
                      );
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
      ),
    );
  }

  Widget _buildChapterScoreChart(List<ChapterReportDto> chapters) {
    if (chapters.isEmpty) {
      return Card(child: Padding(
        padding: const EdgeInsets.all(16),
        child: Text('Chưa có dữ liệu', style: Theme.of(context).textTheme.bodyMedium),
      ));
    }
    final maxVal = chapters.fold<double>(0, (m, c) => c.averageScore > m ? c.averageScore : m);
    final bars = chapters.asMap().entries.map((e) {
      return BarChartGroupData(
        x: e.key,
        barRods: [
          BarChartRodData(
            toY: e.value.averageScore,
            color: Colors.teal,
            width: 20,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
          ),
        ],
      );
    }).toList();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: SizedBox(
          height: 250,
          child: BarChart(
            BarChartData(
              alignment: BarChartAlignment.spaceAround,
              maxY: (maxVal * 1.3).clamp(10, 100.0),
              barTouchData: BarTouchData(
                touchTooltipData: BarTouchTooltipData(
                  getTooltipItem: (group, _, rod, __) {
                    final c = chapters[group.x];
                    return BarTooltipItem(
                      '${c.chapterTitle}\nĐiểm TB: ${c.averageScore.toStringAsFixed(1)}%\nLượt: ${c.totalAttempts}',
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
                    reservedSize: 28,
                    getTitlesWidget: (value, meta) {
                      final idx = value.toInt();
                      if (idx < 0 || idx >= chapters.length) return const SizedBox();
                      final title = chapters[idx].chapterTitle;
                      return Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Text(
                          title.length > 12 ? '${title.substring(0, 12)}...' : title,
                          style: const TextStyle(fontSize: 9),
                        ),
                      );
                    },
                  ),
                ),
                leftTitles: AxisTitles(
                  sideTitles: SideTitles(showTitles: true, reservedSize: 36,
                    getTitlesWidget: (v, _) => Text('${v.toInt()}%', style: const TextStyle(fontSize: 10)),
                  ),
                ),
                topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
              ),
              borderData: FlBorderData(show: false),
              gridData: FlGridData(show: true, drawVerticalLine: false, horizontalInterval: 20),
              barGroups: bars,
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildUserGrowthChart(List<DailyActivityDto> activities) {
    final display = _getFilteredActivities(activities);
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
            width: display.length > 20 ? 8 : 16,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
          ),
        ],
      );
    }).toList();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: SizedBox(
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
                  sideTitles: SideTitles(showTitles: true, reservedSize: 28, interval: display.length > 20 ? 2 : 1,
                    getTitlesWidget: (value, meta) {
                      final idx = value.toInt();
                      if (idx < 0 || idx >= display.length) return const SizedBox();
                      final date = display[idx].date;
                      final parts = date.split('-');
                      final label = parts.length >= 3 ? '${parts[2]}/${parts[1]}' : date;
                      return Padding(padding: const EdgeInsets.only(top: 4), child: Text(label, style: const TextStyle(fontSize: 8)));
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
      ),
    );
  }

  Widget _buildLowScoreLessons(List<ChapterReportDto> chapters) {
    final lowScore = chapters.where((c) => c.averageScore < 70).toList();
    if (lowScore.isEmpty) {
      return Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              const Icon(Icons.check_circle, color: Colors.green),
              const SizedBox(width: 8),
              Text('Tất cả chương đều có điểm trung bình trên 70%',
                  style: Theme.of(context).textTheme.bodyMedium),
            ],
          ),
        ),
      );
    }
    return Column(
      children: lowScore
          .map((c) => Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundColor: Colors.red.withAlpha(25),
                    child: const Icon(Icons.warning, color: Colors.red),
                  ),
                  title: Text(c.chapterTitle),
                  subtitle: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Điểm TB: ${c.averageScore.toStringAsFixed(1)}%'),
                      Text('Tỷ lệ hoàn thành: ${c.completionRate.toStringAsFixed(0)}%'),
                    ],
                  ),
                  trailing: Text(
                    '${c.averageScore.toStringAsFixed(0)}%',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: c.averageScore < 50 ? Colors.red : Colors.orange,
                    ),
                  ),
                ),
              ))
          .toList(),
    );
  }

  List<DailyActivityDto> _getFilteredActivities(List<DailyActivityDto> activities) {
    final days = int.tryParse(_dateFilter) ?? 7;
    if (activities.length <= days) return activities;
    return activities.sublist(activities.length - days);
  }
}

class _SummaryCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _SummaryCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.all(4),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(height: 8),
            Text(value,
                style: Theme.of(context)
                    .textTheme
                    .headlineSmall
                    ?.copyWith(fontWeight: FontWeight.bold)),
            Text(label, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    );
  }
}
