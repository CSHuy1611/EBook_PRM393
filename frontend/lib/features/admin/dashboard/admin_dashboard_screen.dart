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
  String? _selectedUserId;
  List<AdminUserDto> _students = [];

  @override
  void initState() {
    super.initState();
    _fetchStudents();
  }

  Future<void> _fetchStudents() async {
    try {
      final response = await ApiClient.instance.get('/admin/users');
      final data = response.data;
      final list = data is List ? data : (data is Map && data['data'] is List ? data['data'] : []);
      final allUsers = (list as List).map((e) => AdminUserDto.fromJson(e as Map<String, dynamic>)).toList();
      _students = allUsers.where((u) => u.role == 'Student').toList();
      _fetchData();
    } catch (e) {
      _fetchData();
    }
  }

  Future<void> _fetchData() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      var path = '/admin/reports/overview';
      if (_selectedUserId != null) {
        path += '?userId=$_selectedUserId';
      }
      final response = await ApiClient.instance.get(path);
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
          final isWide = constraints.maxWidth > 700;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _buildStudentSelector(),
              const SizedBox(height: 16),
              _buildStatCards(report, isWide),
              const SizedBox(height: 24),
              _buildDailyActivityChart(report.dailyActivities),
              const SizedBox(height: 24),
              _buildChapterCompletionChart(report.chapterReports),
            ],
          );
        },
      ),
    );
  }

  Widget _buildStudentSelector() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            const Icon(Icons.person_search, size: 20),
            const SizedBox(width: 12),
            Expanded(
              child: DropdownButtonFormField<String?>(
                value: _selectedUserId,
                decoration: const InputDecoration(
                  labelText: 'Lọc theo học sinh',
                  border: OutlineInputBorder(),
                  isDense: true,
                  contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                ),
                items: [
                  const DropdownMenuItem(value: null, child: Text('Tất cả học sinh')),
                  ..._students.map((s) => DropdownMenuItem(
                    value: s.id,
                    child: Text('${s.name} (${s.email})'),
                  )),
                ],
                onChanged: (val) {
                  setState(() => _selectedUserId = val);
                  _fetchData();
                },
              ),
            ),
          ],
        ),
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
    ];
    return isWide
        ? Row(children: cards.map((c) => Expanded(child: c)).toList())
        : Column(children: cards);
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
            width: isWideScreen() ? 20 : 12,
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
            Text('Hoạt động hàng ngày', style: Theme.of(context).textTheme.titleMedium),
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
                          '${d.date}\nSố lượng: ${d.quizCount}',
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
            width: isWide ? 24 : 16,
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
            Text('Tỷ lệ hoàn thành theo chương', style: Theme.of(context).textTheme.titleMedium),
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
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: color.withAlpha(25),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: color, size: 28),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: Theme.of(context).textTheme.bodySmall),
                  const SizedBox(height: 4),
                  Text(value,
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
