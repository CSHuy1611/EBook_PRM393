import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class LessonsScreen extends StatefulWidget {
  final String chapterId;

  const LessonsScreen({super.key, required this.chapterId});

  @override
  State<LessonsScreen> createState() => _LessonsScreenState();
}

class _LessonsScreenState extends State<LessonsScreen> {
  List<LessonModel>? _lessons;
  bool _isLoading = true;
  String? _error;
  int _lastVersion = -1;

  @override
  void initState() {
    super.initState();
    _fetchLessons();
  }

  Future<void> _fetchLessons() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/chapters/${widget.chapterId}/lessons');
      final List<dynamic> data = response.data as List<dynamic>;
      _lessons = data.map((e) => LessonModel.fromJson(e as Map<String, dynamic>)).toList();
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
      if (_lessons != null && !_isLoading) _fetchLessons();
    }
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải bài học...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchLessons);
    if (_lessons == null || _lessons!.isEmpty) {
      return const Center(child: Text('Chưa có bài học nào'));
    }
    return RefreshIndicator(
      onRefresh: _fetchLessons,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _lessons!.length,
        itemBuilder: (context, index) {
          final lesson = _lessons![index];
          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: () => context.push('/student/lessons/${lesson.id}'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: lesson.isCompleted
                            ? Colors.green.withAlpha(30)
                            : Theme.of(context).colorScheme.primaryContainer,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Center(
                        child: lesson.isCompleted
                            ? const Icon(Icons.check_circle, color: Colors.green, size: 24)
                            : Text(
                                '${lesson.orderIndex + 1}',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  color: Theme.of(context).colorScheme.onPrimaryContainer,
                                ),
                              ),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            lesson.title,
                            style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
                          ),
                          if (lesson.bestScore > 0) ...[
                            const SizedBox(height: 4),
                            Text(
                              'Điểm cao nhất: ${lesson.bestScore % 1 == 0 ? lesson.bestScore.toInt().toString() : lesson.bestScore.toStringAsFixed(1)}',
                              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: Theme.of(context).colorScheme.onSurfaceVariant,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                    const Icon(Icons.chevron_right, color: Colors.grey),
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
