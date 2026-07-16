import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/student_feature_api.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';

class BadgesScreen extends StatefulWidget {
  const BadgesScreen({super.key});
  @override
  State<BadgesScreen> createState() => _BadgesScreenState();
}

class _BadgesScreenState extends State<BadgesScreen> with SingleTickerProviderStateMixin {
  BadgeCollectionModel? _collection;
  String? _error;
  bool _loading = true;
  late final TabController _tabs;

  @override
  void initState() { super.initState(); _tabs = TabController(length: 3, vsync: this); _load(); }
  @override
  void dispose() { _tabs.dispose(); super.dispose(); }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      await StudentFeatureApi.instance.reconcileBadges();
      final collection = await StudentFeatureApi.instance.getBadges();
      if (mounted) setState(() { _collection = collection; _loading = false; });
    } catch (error) {
      if (mounted) setState(() { _error = error is DioException ? ApiClient.mapDioErrorToMessage(error) : error.toString(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Scaffold(body: AppLoadingWidget(message: 'Đang tải huy hiệu...'));
    if (_error != null) return Scaffold(body: AppErrorWidget(message: _error!, onRetry: _load));
    final all = _collection!.items;
    return Scaffold(
      appBar: AppBar(title: const Text('Huy hiệu')),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 6),
          child: Align(alignment: Alignment.centerLeft, child: Text('Đã đạt ${_collection!.earnedCount}/${_collection!.totalCount}', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold))),
        ),
        TabBar(controller: _tabs, tabs: const [Tab(text: 'Đã đạt'), Tab(text: 'Đang tiến tới'), Tab(text: 'Chưa đủ điều kiện')]),
        Expanded(child: TabBarView(controller: _tabs, children: [
          _BadgeList(items: all.where((item) => item.status == 'Earned').toList(), empty: 'Bạn chưa nhận huy hiệu nào.', onRefresh: _load),
          _BadgeList(items: all.where((item) => item.status == 'InProgress').toList(), empty: 'Chưa có huy hiệu nào đang tiến tới.', onRefresh: _load),
          _BadgeList(items: all.where((item) => item.status == 'Locked').toList(), empty: 'Không còn huy hiệu bị khóa.', onRefresh: _load),
        ])),
      ]),
    );
  }
}

class _BadgeList extends StatelessWidget {
  final List<BadgeCollectionItemModel> items;
  final String empty;
  final Future<void> Function() onRefresh;
  const _BadgeList({required this.items, required this.empty, required this.onRefresh});

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: onRefresh,
    child: items.isEmpty
        ? ListView(children: [SizedBox(height: 240, child: Center(child: Text(empty)))])
        : ListView.builder(padding: const EdgeInsets.all(16), itemCount: items.length, itemBuilder: (_, index) => _BadgeCard(item: items[index])),
  );
}

class _BadgeCard extends StatelessWidget {
  final BadgeCollectionItemModel item;
  const _BadgeCard({required this.item});

  bool get _earned => item.status == 'Earned';

  @override
  Widget build(BuildContext context) {
    final color = _earned ? Colors.amber.shade700 : item.status == 'InProgress' ? Colors.deepPurple : Colors.grey;
    final progress = (item.progressPercentage / 100).clamp(0.0, 1.0);
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          CircleAvatar(radius: 28, backgroundColor: color.withAlpha(30), child: Icon(Icons.workspace_premium, color: color, size: 32)),
          const SizedBox(width: 14),
          Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(item.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 3),
            Text(item.description),
            const SizedBox(height: 10),
            if (_earned)
              Text(item.earnedAt == null ? 'Đã nhận huy hiệu' : 'Nhận ngày ${DateFormat('dd/MM/yyyy').format(item.earnedAt!.toLocal())}', style: TextStyle(color: color, fontWeight: FontWeight.w600))
            else ...[
              Text(item.requirement),
              const SizedBox(height: 6),
              LinearProgressIndicator(value: progress, minHeight: 7, borderRadius: BorderRadius.circular(6)),
              const SizedBox(height: 4),
              Align(alignment: Alignment.centerRight, child: Text('${item.progressPercentage.toStringAsFixed(0)}%')),
            ],
          ])),
        ]),
      ),
    );
  }
}
