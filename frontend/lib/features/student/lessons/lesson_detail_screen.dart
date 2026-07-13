import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:math_ibook/core/math/math_text.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/local_prefs_service.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/features/student/simulation/simulation_widget.dart';

class LessonDetailScreen extends StatefulWidget {
  final String lessonId;

  const LessonDetailScreen({super.key, required this.lessonId});

  @override
  State<LessonDetailScreen> createState() => _LessonDetailScreenState();
}

class _LessonDetailScreenState extends State<LessonDetailScreen> {
  LessonModel? _lesson;
  bool _isLoading = true;
  String? _error;
  double _fontScale = 1.0;

  @override
  void initState() {
    super.initState();
    _fontScale = LocalPrefsService().getFontScale();
    _fetchLesson();
  }

  Future<void> _fetchLesson() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/lessons/${widget.lessonId}');
      final data = response.data as Map<String, dynamic>;
      _lesson = LessonModel.fromJson(data);
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
    if (_isLoading) return const Scaffold(body: AppLoadingWidget(message: 'Đang tải bài học...'));
    if (_error != null) {
      return Scaffold(
        body: AppErrorWidget(message: _error!, onRetry: _fetchLesson),
      );
    }
    if (_lesson == null) return const Scaffold(body: Center(child: Text('Không tìm thấy bài học')));

    final lesson = _lesson!;
    final textStyle = Theme.of(context).textTheme.bodyLarge?.copyWith(fontSize: 16 * _fontScale);

    return Scaffold(
      appBar: AppBar(
        title: Text(lesson.title),
        actions: [
          PopupMenuButton<double>(
            icon: const Icon(Icons.text_fields),
            onSelected: (scale) {
              setState(() => _fontScale = scale);
              LocalPrefsService().setFontScale(scale);
            },
            itemBuilder: (context) => [
              const PopupMenuItem(value: 0.8, child: Text('Nhỏ')),
              const PopupMenuItem(value: 1.0, child: Text('Vừa')),
              const PopupMenuItem(value: 1.2, child: Text('Lớn')),
              const PopupMenuItem(value: 1.5, child: Text('Rất lớn')),
            ],
          ),
        ],
      ),
      floatingActionButton: lesson.questions.isNotEmpty
          ? FloatingActionButton.extended(
              onPressed: () => context.push('/student/quiz/${lesson.id}'),
              icon: const Icon(Icons.quiz),
              label: const Text('Làm bài kiểm tra'),
            )
          : null,
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            MathText(
              lesson.contentBody,
              textStyle: textStyle,
            ),
            if (lesson.simulationType.isNotEmpty) ...[
              const SizedBox(height: 24),
              SimulationWidget(simulationType: lesson.simulationType),
            ],
            if (lesson.questions.isNotEmpty) ...[
              const SizedBox(height: 24),
              Divider(),
              const SizedBox(height: 8),
              Text(
                'Câu hỏi tham khảo',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 12),
              ...lesson.questions.asMap().entries.map((entry) {
                final idx = entry.key;
                final q = entry.value;
                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Câu ${idx + 1}: ',
                              style: const TextStyle(fontWeight: FontWeight.w600),
                            ),
                            Expanded(
                              child: MathText(
                                q.questionText,
                                textStyle: const TextStyle(fontWeight: FontWeight.w600),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        ...q.options.asMap().entries.map((opt) {
                          final letter = String.fromCharCode(65 + opt.key);
                          return Padding(
                            padding: const EdgeInsets.symmetric(vertical: 2),
                            child: Row(
                              children: [
                                Text('$letter. ', style: const TextStyle(fontWeight: FontWeight.bold)),
                                Expanded(child: MathText(opt.value)),
                              ],
                            ),
                          );
                        }),
                      ],
                    ),
                  ),
                );
              }),
            ],
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }
}
