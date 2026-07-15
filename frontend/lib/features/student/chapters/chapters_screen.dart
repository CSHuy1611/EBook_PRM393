import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';

class ChaptersScreen extends StatefulWidget {
  const ChaptersScreen({super.key});

  @override
  State<ChaptersScreen> createState() => _ChaptersScreenState();
}

class _ChaptersScreenState extends State<ChaptersScreen> {
  List<ChapterModel>? _chapters;
  bool _isLoading = true;
  String? _error;
  int _lastVersion = -1;

  @override
  void initState() {
    super.initState();
    _fetchChapters();
  }

  Future<void> _fetchChapters() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/chapters');
      final List<dynamic> data = response.data as List<dynamic>;
      _chapters = data.map((e) => ChapterModel.fromJson(e as Map<String, dynamic>)).toList();
      await LocalDbService().cacheChapters(_chapters!.map((chapter) => chapter.toJson()).toList());
      setState(() => _isLoading = false);
    } catch (e) {
      final cached = await LocalDbService().getCachedChapters();
      if (cached.isNotEmpty) {
        setState(() { _chapters = cached.map(ChapterModel.fromJson).toList(); _isLoading = false; });
      } else {
        setState(() { _error = e.toString(); _isLoading = false; });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final version = context.watch<ProgressNotifier>().version;
    if (version != _lastVersion) {
      _lastVersion = version;
      if (_chapters != null && !_isLoading) _fetchChapters();
    }
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải chương học...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchChapters);
    if (_chapters == null || _chapters!.isEmpty) {
      return const Center(child: Text('Chưa có chương học nào'));
    }
    return RefreshIndicator(
      onRefresh: _fetchChapters,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _chapters!.length,
        itemBuilder: (context, index) {
          final chapter = _chapters![index];
          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: () => context.push('/student/chapters/${chapter.id}'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            chapter.title,
                            style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                          ),
                        ),
                        if (chapter.completionPercentage >= 100)
                          const Icon(Icons.check_circle, color: Colors.green, size: 20),
                      ],
                    ),
                    if (chapter.description.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(
                        chapter.description,
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Theme.of(context).colorScheme.onSurfaceVariant,
                        ),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: LinearProgressIndicator(
                            value: chapter.completionPercentage / 100,
                            minHeight: 8,
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Text(
                          '${chapter.completionPercentage.toStringAsFixed(0)}%',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${chapter.lessonCount} bài học',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
