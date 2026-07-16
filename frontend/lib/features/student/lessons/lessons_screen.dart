import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:dio/dio.dart';

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
        _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
        _isLoading = false;
      });
    }
  }

  Future<void> _fetchChapterQuiz() async {
    try {
      final response = await ApiClient.instance.get('/quizzes/chapter/${widget.chapterId}');
      if (mounted) {
        setState(() {
          _chapterQuizData = response.data as Map<String, dynamic>?;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _chapterQuizData = null;
        });
      }
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
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
        itemCount: _lessons!.length + (_chapterQuizData != null ? 1 : 0),
        itemBuilder: (context, index) {
          if (_chapterQuizData != null && index == 0) {
            return _buildChapterQuizCard();
          }
          final lessonIndex = _chapterQuizData != null ? index - 1 : index;
          final lesson = _lessons![lessonIndex];
          final status = lesson.status;
          return Container(
            margin: const EdgeInsets.only(bottom: 10),
            child: Material(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(12),
              child: InkWell(
                borderRadius: BorderRadius.circular(12),
                onTap: () => context.push('/student/lessons/${lesson.id}'),
                child: Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: const Color(0xFFE2E8F0)),
                  ),
                  child: Row(
                    children: [
                      Container(
                        width: 42,
                        height: 42,
                        decoration: BoxDecoration(
                          color: status == 'Passed'
                              ? const Color(0xFF10B981).withAlpha(20)
                              : (status == 'InProgress'
                                  ? const Color(0xFF3B82F6).withAlpha(20)
                                  : Theme.of(context).colorScheme.surfaceContainerHighest),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Center(
                          child: status == 'Passed'
                              ? const Icon(Icons.check_rounded, color: Color(0xFF10B981), size: 22)
                              : Text(
                                  '${lesson.orderIndex}',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: status == 'InProgress'
                                        ? const Color(0xFF3B82F6)
                                        : const Color(0xFF64748B),
                                    fontSize: 16,
                                  ),
                                ),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              lesson.title,
                              style: TextStyle(fontWeight: FontWeight.w600, fontSize: 15, color: Theme.of(context).colorScheme.onSurface),
                            ),
                            const SizedBox(height: 4),
                            Row(
                              children: [
                                _buildStatusChip(status),
                                if (lesson.bestScore > 0) ...[
                                  const SizedBox(width: 8),
                                  Icon(Icons.score_rounded, size: 14, color: const Color(0xFFF59E0B)),
                                  const SizedBox(width: 4),
                                  Text(
                                    'Điểm: ${lesson.bestScore % 1 == 0 ? lesson.bestScore.toInt().toString() : lesson.bestScore.toStringAsFixed(1)}',
                                    style: const TextStyle(fontSize: 12, color: Color(0xFFF59E0B), fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ],
                            ),
                          ],
                        ),
                      ),
                      Container(
                        width: 26,
                        height: 26,
                        decoration: BoxDecoration(
                          color: const Color(0xFF3B82F6).withAlpha(15),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Icon(Icons.chevron_right_rounded, size: 16, color: Color(0xFF3B82F6)),
                      ),
                    ],
                  ),
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

    Color statusColor;
    IconData statusIcon;
    String statusText;
    VoidCallback? onTap;

    if (status == 'Passed') {
      statusColor = const Color(0xFF10B981);
      statusIcon = Icons.check_circle_rounded;
      statusText = 'Đã hoàn thành';
      onTap = () => _startChapterQuiz();
    } else if (isUnlocked) {
      statusColor = const Color(0xFF3B82F6);
      statusIcon = Icons.lock_open_rounded;
      statusText = 'Sẵn sàng làm bài';
      onTap = () => _startChapterQuiz();
    } else {
      statusColor = const Color(0xFFF59E0B);
      statusIcon = Icons.lock_rounded;
      statusText = 'Chưa mở khóa';
      onTap = null;
    }

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      child: Material(
        color: statusColor.withAlpha(8),
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: onTap,
          child: Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: statusColor.withAlpha(60)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: BoxDecoration(
                        color: statusColor.withAlpha(20),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Icon(statusIcon, color: statusColor, size: 24),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Bài kiểm tra cuối chương',
                            style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Theme.of(context).colorScheme.onSurface),
                          ),
                          Text(
                            statusText,
                            style: TextStyle(color: statusColor, fontWeight: FontWeight.w600, fontSize: 13),
                          ),
                        ],
                      ),
                    ),
                    if (bestScore != null)
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                        decoration: BoxDecoration(
                          color: const Color(0xFF10B981).withAlpha(20),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Text(
                          'Điểm: ${(bestScore as num).toStringAsFixed(1)}',
                          style: const TextStyle(color: Color(0xFF10B981), fontWeight: FontWeight.bold, fontSize: 12),
                        ),
                      ),
                  ],
                ),
                if (!isUnlocked && missingLessons.isNotEmpty) ...[
                  const SizedBox(height: 14),
                  Divider(height: 1, color: statusColor.withAlpha(50)),
                  const SizedBox(height: 10),
                  Text(
                    'Cần hoàn thành các bài học sau:',
                    style: TextStyle(color: statusColor, fontWeight: FontWeight.w600, fontSize: 12),
                  ),
                  const SizedBox(height: 6),
                  ...missingLessons.map((lesson) {
                    final title = lesson is Map ? lesson['title'] ?? '' : lesson.toString();
                    final lessonId = lesson is Map ? lesson['id'] ?? '' : '';
                    return Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: InkWell(
                        onTap: lessonId.isNotEmpty ? () => context.push('/student/lessons/$lessonId') : null,
                        child: Row(
                          children: [
                            Icon(Icons.radio_button_unchecked_rounded, size: 16, color: statusColor),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                title,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: statusColor,
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
                  const SizedBox(height: 14),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: () => _startChapterQuiz(),
                      icon: const Icon(Icons.quiz_rounded, size: 18),
                      label: const Text('Làm bài kiểm tra chương'),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }

  void _startChapterQuiz() {
    context.push('/student/chapter-quiz/${widget.chapterId}');
  }

  Widget _buildStatusChip(String status) {
    Color bgColor;
    Color textColor;
    String label;
    IconData icon;

    switch (status) {
      case 'Passed':
        bgColor = const Color(0xFF10B981).withAlpha(20);
        textColor = const Color(0xFF10B981);
        label = 'Đã pass';
        icon = Icons.check_circle_rounded;
        break;
      case 'InProgress':
        bgColor = const Color(0xFF3B82F6).withAlpha(20);
        textColor = const Color(0xFF3B82F6);
        label = 'Đang học';
        icon = Icons.pending_rounded;
        break;
      case 'NotStarted':
      default:
        bgColor = const Color(0xFF94A3B8).withAlpha(20);
        textColor = const Color(0xFF64748B);
        label = 'Chưa học';
        icon = Icons.radio_button_unchecked_rounded;
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: textColor, size: 12),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: textColor),
          ),
        ],
      ),
    );
  }
}
