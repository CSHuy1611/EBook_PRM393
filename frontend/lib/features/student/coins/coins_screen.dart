import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/student_feature_api.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';

class CoinsScreen extends StatefulWidget {
  const CoinsScreen({super.key});

  @override
  State<CoinsScreen> createState() => _CoinsScreenState();
}

class _CoinsScreenState extends State<CoinsScreen> {
  final _scrollController = ScrollController();
  final _items = <CoinTransactionModel>[];
  CoinHistoryModel? _history;
  bool _loading = true;
  bool _loadingMore = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_loadMoreIfNeeded);
    _loadFirstPage();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _loadFirstPage() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final history = await StudentFeatureApi.instance.getCoins();
      if (!mounted) return;
      setState(() {
        _history = history;
        _items
          ..clear()
          ..addAll(history.items);
        _loading = false;
      });
    } catch (error) {
      if (mounted) setState(() { _error = error is DioException ? ApiClient.mapDioErrorToMessage(error) : error.toString(); _loading = false; });
    }
  }

  void _loadMoreIfNeeded() {
    if (_scrollController.position.extentAfter < 280) _loadMore();
  }

  Future<void> _loadMore() async {
    final history = _history;
    if (_loadingMore || history == null || _items.length >= history.totalItems) return;
    setState(() => _loadingMore = true);
    try {
      final next = await StudentFeatureApi.instance.getCoins(page: history.page + 1);
      if (!mounted) return;
      setState(() { _history = next; _items.addAll(next.items); });
    } finally {
      if (mounted) setState(() => _loadingMore = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Scaffold(body: AppLoadingWidget(message: 'Đang tải xu...'));
    if (_error != null) return Scaffold(body: AppErrorWidget(message: _error!, onRetry: _loadFirstPage));
    final totalCoins = _history?.totalCoins ?? 0;
    return Scaffold(
      appBar: AppBar(title: const Text('Xu của bạn')),
      body: RefreshIndicator(
        onRefresh: _loadFirstPage,
        child: ListView.builder(
          controller: _scrollController,
          padding: const EdgeInsets.all(16),
          itemCount: _items.length + 2,
          itemBuilder: (context, index) {
            if (index == 0) return _TotalCoinsCard(totalCoins: totalCoins);
            if (index == 1) return const Padding(
              padding: EdgeInsets.only(top: 24, bottom: 8),
              child: Text('Lịch sử nhận xu', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            );
            final itemIndex = index - 2;
            if (itemIndex >= _items.length) {
              return _loadingMore
                  ? const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator()))
                  : const SizedBox(height: 32);
            }
            return _TransactionTile(item: _items[itemIndex]);
          },
        ),
      ),
    );
  }
}

class _TotalCoinsCard extends StatelessWidget {
  final int totalCoins;
  const _TotalCoinsCard({required this.totalCoins});

  @override
  Widget build(BuildContext context) => Card(
    color: Colors.amber.shade50,
    child: Padding(
      padding: const EdgeInsets.all(20),
      child: Row(children: [
        const Icon(Icons.monetization_on, size: 48, color: Colors.amber),
        const SizedBox(width: 16),
        Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const Text('Tổng xu hiện có'),
          Text(NumberFormat.decimalPattern('vi').format(totalCoins),
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold)),
        ]),
      ]),
    ),
  );
}

class _TransactionTile extends StatelessWidget {
  final CoinTransactionModel item;
  const _TransactionTile({required this.item});

  ({IconData icon, String title, Color color}) get _source => switch (item.sourceType) {
    'lesson_quiz' => (icon: Icons.quiz, title: 'Quiz bài học', color: Colors.blue),
    'chapter_quiz' => (icon: Icons.menu_book, title: 'Quiz chương', color: Colors.deepPurple),
    'badge_unlock' => (icon: Icons.workspace_premium, title: 'Huy hiệu', color: Colors.orange),
    _ => (icon: Icons.card_giftcard, title: 'Phần thưởng', color: Colors.green),
  };

  @override
  Widget build(BuildContext context) {
    final source = _source;
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: CircleAvatar(backgroundColor: source.color.withAlpha(30), child: Icon(source.icon, color: source.color)),
        title: Text(source.title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text('${item.description}\n${DateFormat('dd/MM/yyyy HH:mm').format(item.createdAt.toLocal())}'),
        isThreeLine: true,
        trailing: Text('+${NumberFormat.decimalPattern('vi').format(item.amount)}',
          style: const TextStyle(color: Colors.green, fontWeight: FontWeight.bold)),
      ),
    );
  }
}
