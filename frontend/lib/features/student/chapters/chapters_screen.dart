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
  String _quizStatusText(ChapterModel chapter) {
    switch (chapter.chapterQuizStatus) {
      case 'Passed': return 'Đã hoàn thành bài kiểm tra chương';
      case 'Unlocked': return 'Bài kiểm tra chương đã mở khóa';
      case 'Locked': return 'Chưa đủ điều kiện làm bài kiểm tra chương';
      default: return '';
    }
  }

  Color _quizStatusColor(ChapterModel chapter) {
    switch (chapter.chapterQuizStatus) {
      case 'Passed': return Colors.green;
      case 'Unlocked': return Colors.blue;
      case 'Locked': return Colors.orange;
      default: return Colors.grey;
    }
  }
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
          final showLocked = !chapter.isUnlocked;

          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            color: showLocked ? Colors.grey.withAlpha(30) : null,
            child: InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: showLocked
                  ? () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Chương này chưa được mở khóa. Hoàn thành bài kiểm tra chương trước để mở khóa.')),
                      );
                    }
                  : () => context.push('/student/chapters/${chapter.id}'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Stack(
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                chapter.title,
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                                  fontWeight: FontWeight.bold,
                                  color: showLocked ? Colors.grey : null,
                                ),
                              ),
                            ),
                            if (chapter.isQuizPassed)
                              const Icon(Icons.check_circle, color: Colors.green, size: 20)
                            else if (chapter.isQuizUnlocked)
                              const Icon(Icons.lock_open, color: Colors.blue, size: 20)
                            else if (showLocked)
                              const Icon(Icons.lock, color: Colors.grey, size: 20),
                          ],
                        ),
                        if (chapter.description.isNotEmpty) ...[
                          const SizedBox(height: 4),
                          Text(
                            chapter.description,
                            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                              color: showLocked ? Colors.grey : Theme.of(context).colorScheme.onSurfaceVariant,
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
                                color: showLocked ? Colors.grey : null,
                              ),
                            ),
                            const SizedBox(width: 12),
                            Text(
                              '${chapter.completionPercentage.toStringAsFixed(0)}%',
                              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                fontWeight: FontWeight.w600,
                                color: showLocked ? Colors.grey : null,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${chapter.lessonCount} bài học',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: showLocked ? Colors.grey : Theme.of(context).colorScheme.onSurfaceVariant,
                          ),
                        ),
                        if (showLocked) ...[
                          const SizedBox(height: 4),
                          Text(
                            'Bị khóa - cần hoàn thành chương trước',
                            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: Colors.orange.shade700,
                              fontWeight: FontWeight.w600,
                              fontSize: 11,
                            ),
                          ),
                        ] else if (chapter.chapterQuizStatus != 'Unavailable') ...[
                          const SizedBox(height: 2),
                          Text(
                            _quizStatusText(chapter),
                            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: _quizStatusColor(chapter),
                              fontWeight: FontWeight.w600,
                              fontSize: 11,
                            ),
                          ),
                        ],
                      ],
                    ),
                    if (showLocked)
                      Positioned.fill(
                        child: IgnorePointer(
                          child: Container(color: Colors.transparent),
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
