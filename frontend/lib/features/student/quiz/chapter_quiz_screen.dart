import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import 'package:provider/provider.dart';
import 'package:uuid/uuid.dart';
import 'package:math_ibook/core/math/math_text.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/core/models/quiz_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';

class ChapterQuizScreen extends StatefulWidget {
  final String chapterId;

  const ChapterQuizScreen({super.key, required this.chapterId});

  @override
  State<ChapterQuizScreen> createState() => _ChapterQuizScreenState();
}

class _ChapterQuizScreenState extends State<ChapterQuizScreen> {
  List<StudentQuestion> _questions = [];
  bool _isLoading = true;
  bool _isSubmitting = false;
  String? _error;
  String? _quizId;
  String? _title;

  int _currentQuestion = 0;
  final Map<String, int> _answers = {};
  int _durationSeconds = 0;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _fetchQuiz();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _fetchQuiz() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/quizzes/chapter/${widget.chapterId}');
      final data = response.data as Map<String, dynamic>;
      _quizId = data['id'] ?? '';
      _title = data['title'] ?? 'Bài kiểm tra chương';

      final questionsRaw = data['questions'] as List? ?? [];
      _questions = questionsRaw.map((q) => StudentQuestion.fromJson(q as Map<String, dynamic>)).toList();

      if (_questions.isEmpty) {
        setState(() {
          _error = 'Bài kiểm tra chưa có câu hỏi';
          _isLoading = false;
        });
        return;
      }

      setState(() => _isLoading = false);
      _startTimer();
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  void _startTimer() {
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (mounted) setState(() => _durationSeconds++);
    });
  }

  String _formatDuration(int seconds) {
    final min = seconds ~/ 60;
    final sec = seconds % 60;
    return '${min.toString().padLeft(2, '0')}:${sec.toString().padLeft(2, '0')}';
  }

  bool get _allAnswered => _answers.length == _questions.length;

  void _submitQuiz() async {
    if (!_allAnswered) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng trả lời tất cả câu hỏi trước khi nộp bài')),
      );
      return;
    }

    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xác nhận nộp bài'),
        content: Text('Bạn đã trả lời ${_answers.length} câu hỏi. Bạn có chắc muốn nộp bài không?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Hủy')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Nộp bài')),
        ],
      ),
    );

    if (confirm != true) return;
    if (_isSubmitting) return;

    setState(() => _isSubmitting = true);
    _timer?.cancel();
    final dto = QuizSubmitDto(
      quizId: _quizId,
      clientAttemptId: const Uuid().v4(),
      durationSeconds: _durationSeconds,
      answers: _answers.entries.map((entry) => AnswerDto(questionId: entry.key, selectedOption: entry.value)).toList(),
      clientCreatedAt: DateTime.now().toUtc().toIso8601String(),
    );

    try {
      final response = await ApiClient.instance.post(
        '/quiz-attempts',
        data: dto.toJson(),
      );

      final resultData = response.data as Map<String, dynamic>;
      final result = QuizResultDto.fromJson(resultData);

      if (result.coinsEarned > 0 && mounted) {
        context.read<AuthProvider>().addCoins(result.coinsEarned);
      }

      if (mounted) {
        context.read<ProgressNotifier>().notifyProgressChanged();
        context.pushReplacement(
          '/student/quiz/result/${resultData['id'] ?? ''}',
          extra: result,
        );
      }
    } catch (e) {
      if (_isNetworkError(e)) {
        final userId = context.read<AuthProvider>().currentUser?.id;
        if (userId != null && userId.isNotEmpty) {
          await LocalDbService().queueQuizAttempt(
            userId: userId, lessonId: '', quizId: dto.quizId, clientAttemptId: dto.clientAttemptId,
            durationSeconds: dto.durationSeconds, answers: dto.answers.map((answer) => answer.toJson()).toList(), createdAt: DateTime.parse(dto.clientCreatedAt),
          );
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đã lưu bài làm chương. Server sẽ chấm điểm khi đồng bộ lại.')));
            context.pop();
          }
          return;
        }
      }
      setState(() => _isSubmitting = false);
      if (mounted) {
        final detail = (e is DioException && e.response?.data != null)
            ? 'Lỗi: ${e.response?.data}'
            : 'Lỗi khi nộp bài: $e';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(detail)),
        );
      }
    }
  }

  bool _isNetworkError(Object error) => error is DioException &&
      (error.type == DioExceptionType.connectionError ||
       error.type == DioExceptionType.connectionTimeout ||
       error.type == DioExceptionType.receiveTimeout ||
       error.type == DioExceptionType.sendTimeout);

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const Scaffold(body: AppLoadingWidget(message: 'Đang tải bài kiểm tra chương...'));
    if (_error != null) {
      return Scaffold(body: AppErrorWidget(message: _error!, onRetry: _fetchQuiz));
    }
    if (_questions.isEmpty) {
      return Scaffold(
        appBar: AppBar(title: const Text('Bài kiểm tra chương')),
        body: const Center(child: Text('Bài kiểm tra chưa có câu hỏi')),
      );
    }

    final currentQ = _questions[_currentQuestion];

    return Scaffold(
      appBar: AppBar(
        title: Text(_title ?? 'Bài kiểm tra chương'),
        actions: [
          Center(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Text(
                _formatDuration(_durationSeconds),
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, fontFamily: 'monospace'),
              ),
            ),
          ),
        ],
      ),
      body: _isSubmitting
          ? const AppLoadingWidget(message: 'Đang nộp bài...')
          : Column(
              children: [
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      Text(
                        'Câu ${_currentQuestion + 1} / ${_questions.length}',
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                      ),
                      const Spacer(),
                      Text(
                        'Đã trả lời: ${_answers.length}/${_questions.length}',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                    ],
                  ),
                ),
                LinearProgressIndicator(
                  value: (_currentQuestion + 1) / _questions.length,
                  minHeight: 4,
                ),
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        MathText(
                          currentQ.questionText,
                          textStyle: Theme.of(context).textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 20),
                        ...currentQ.options.asMap().entries.map((entry) {
                          final optIndex = entry.key;
                          final optText = entry.value;
                          final letter = String.fromCharCode(65 + optIndex);
                          final isSelected = _answers[currentQ.id] == optIndex;

                          return Padding(
                            padding: const EdgeInsets.symmetric(vertical: 6),
                            child: InkWell(
                              borderRadius: BorderRadius.circular(12),
                              onTap: () {
                                setState(() => _answers[currentQ.id] = optIndex);
                              },
                              child: Container(
                                width: double.infinity,
                                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                                decoration: BoxDecoration(
                                  color: isSelected
                                      ? Theme.of(context).colorScheme.primaryContainer
                                      : Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(100),
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: isSelected
                                        ? Theme.of(context).colorScheme.primary
                                        : Theme.of(context).colorScheme.outline.withAlpha(60),
                                    width: isSelected ? 2 : 1,
                                  ),
                                ),
                                child: Row(
                                  children: [
                                    Container(
                                      width: 32,
                                      height: 32,
                                      decoration: BoxDecoration(
                                        shape: BoxShape.circle,
                                        color: isSelected
                                            ? Theme.of(context).colorScheme.primary
                                            : Colors.transparent,
                                        border: Border.all(
                                          color: isSelected
                                              ? Theme.of(context).colorScheme.primary
                                              : Theme.of(context).colorScheme.outline,
                                        ),
                                      ),
                                      child: Center(
                                        child: isSelected
                                            ? const Icon(Icons.check, color: Colors.white, size: 18)
                                            : Text(
                                                letter,
                                                style: TextStyle(
                                                  fontWeight: FontWeight.bold,
                                                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                                                ),
                                              ),
                                      ),
                                    ),
                                    const SizedBox(width: 16),
                                    Expanded(child: MathText(optText)),
                                  ],
                                ),
                              ),
                            ),
                          );
                        }),
                      ],
                    ),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.surface,
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withAlpha(20),
                        blurRadius: 8,
                        offset: const Offset(0, -2),
                      ),
                    ],
                  ),
                  child: Row(
                    children: [
                      if (_currentQuestion > 0)
                        OutlinedButton(
                          onPressed: () => setState(() => _currentQuestion--),
                          child: const Text('Câu trước'),
                        )
                      else
                        const SizedBox(),
                      const Spacer(),
                      if (_currentQuestion < _questions.length - 1)
                        FilledButton(
                          onPressed: () => setState(() => _currentQuestion++),
                          child: const Text('Câu tiếp'),
                        )
                      else
                        FilledButton(
                          onPressed: _submitQuiz,
                          child: const Text('Nộp bài'),
                        ),
                    ],
                  ),
                ),
              ],
            ),
    );
  }
}

class StudentQuestion {
  final String id;
  final String questionText;
  final List<String> options;
  final int orderIndex;

  StudentQuestion({
    required this.id,
    required this.questionText,
    this.options = const [],
    this.orderIndex = 0,
  });

  factory StudentQuestion.fromJson(Map<String, dynamic> json) {
    List<String> optionsList = [];
    if (json['options'] != null) {
      if (json['options'] is List) {
        optionsList = (json['options'] as List).map((e) => e.toString()).toList();
      } else if (json['options'] is String) {
        optionsList = (json['options'] as String).split(',').map((e) => e.trim()).toList();
      }
    }
    return StudentQuestion(
      id: json['id'] ?? '',
      questionText: json['questionText'] ?? json['question_text'] ?? '',
      options: optionsList,
      orderIndex: json['orderIndex'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'questionText': questionText,
        'options': options,
        'orderIndex': orderIndex,
      };
}