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

  Map<String, dynamic>? _chapterQuizData;

  @override
  void initState() {
    super.initState();
    _fetchLessons();
    _fetchChapterQuiz();
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

  Future<void> _fetchChapterQuiz() async {
    try {
      final response = await ApiClient.instance.get('/quizzes/chapter/${widget.chapterId}');
      _chapterQuizData = response.data as Map<String, dynamic>?;
    } catch (_) {
      _chapterQuizData = null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final version = context.watch<ProgressNotifier>().version;
    if (version != _lastVersion) {
      _lastVersion = version;
      if (_lessons != null && !_isLoading) {
        _fetchLessons();
        _fetchChapterQuiz();
      }
    }
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải bài học...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchLessons);
    if (_lessons == null || _lessons!.isEmpty) {
      return const Center(child: Text('Chưa có bài học nào'));
    }
    return RefreshIndicator(
      onRefresh: () async {
        await _fetchLessons();
        await _fetchChapterQuiz();
      },
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _lessons!.length + (_chapterQuizData != null ? 1 : 0),
        itemBuilder: (context, index) {
          if (_chapterQuizData != null && index == 0) {
            return _buildChapterQuizCard();
          }
          final lessonIndex = _chapterQuizData != null ? index - 1 : index;
          final lesson = _lessons![lessonIndex];
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

  Widget _buildChapterQuizCard() {
    final data = _chapterQuizData!;
    final isUnlocked = data['isUnlocked'] == true;
    final status = data['status'] ?? 'NotStarted';
    final bestScore = data['bestScore'];
    final missingLessons = (data['missingLessons'] as List?) ?? [];
    final quizId = data['id'] ?? '';

    Color statusColor;
    IconData statusIcon;
    String statusText;
    VoidCallback? onTap;

    if (status == 'Passed') {
      statusColor = Colors.green;
      statusIcon = Icons.check_circle;
      statusText = 'Đã hoàn thành';
      onTap = () => _startChapterQuiz(quizId);
    } else if (isUnlocked) {
      statusColor = Colors.blue;
      statusIcon = Icons.lock_open;
      statusText = 'Sẵn sàng làm bài';
      onTap = () => _startChapterQuiz(quizId);
    } else {
      statusColor = Colors.orange;
      statusIcon = Icons.lock;
      statusText = 'Chưa mở khóa';
      onTap = null;
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      color: statusColor.withAlpha(15),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: statusColor.withAlpha(80)),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(statusIcon, color: statusColor, size: 28),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Bài kiểm tra cuối chương',
                          style: Theme.of(context).textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        Text(
                          statusText,
                          style: TextStyle(
                            color: statusColor,
                            fontWeight: FontWeight.w600,
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (bestScore != null)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.green.withAlpha(30),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        'Điểm: ${(bestScore as num).toStringAsFixed(1)}',
                        style: const TextStyle(
                          color: Colors.green,
                          fontWeight: FontWeight.bold,
                          fontSize: 13,
                        ),
                      ),
                    ),
                ],
              ),
              if (!isUnlocked && missingLessons.isNotEmpty) ...[
                const SizedBox(height: 12),
                const Divider(height: 1),
                const SizedBox(height: 8),
                Text(
                  'Cần hoàn thành các bài học sau:',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Colors.orange.shade800,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 4),
                ...missingLessons.map((lesson) {
                  final title = lesson is Map ? lesson['title'] ?? '' : lesson.toString();
                  final lessonId = lesson is Map ? lesson['id'] ?? '' : '';
                  return Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: InkWell(
                      onTap: lessonId.isNotEmpty
                          ? () => context.push('/student/lessons/$lessonId')
                          : null,
                      child: Row(
                        children: [
                          Icon(Icons.radio_button_unchecked, size: 16, color: Colors.orange.shade400),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              title,
                              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: Colors.orange.shade700,
                                decoration: lessonId.isNotEmpty ? TextDecoration.underline : null,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }),
              ],
              if (isUnlocked) ...[
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: () => _startChapterQuiz(quizId),
                    icon: const Icon(Icons.quiz),
                    label: const Text('Làm bài kiểm tra chương'),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  void _startChapterQuiz(String quizId) {
    context.push('/student/chapter-quiz/${widget.chapterId}');
  }
}
