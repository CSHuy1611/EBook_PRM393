import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/math/math_text.dart';
import 'package:math_ibook/core/math/math_toolbar.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/features/admin/questions_admin/auto_generate_questions_dialog.dart' as math_dialog;

class AdminQuestionsScreen extends StatefulWidget {
  final String? lessonId;
  const AdminQuestionsScreen({super.key, this.lessonId});

  @override
  State<AdminQuestionsScreen> createState() => _AdminQuestionsScreenState();
}

class _AdminQuestionsScreenState extends State<AdminQuestionsScreen> {
  List<QuestionModel> _questions = [];
  String? _selectedLessonId;
  String _lessonTitle = '';
  bool _isLoading = false;
  String? _error;
  List<ChapterModel> _chapters = [];
  String? _selectedChapterId;
  List<LessonModel> _chapterLessons = [];
  bool _isLoadingChapters = false;

  bool _inMathMode(String text, int pos) {
    int count = 0;
    for (int i = 0; i < pos; i++) {
      if (text[i] == r'$' && (i == 0 || text[i - 1] != r'\')) count++;
    }
    return count % 2 == 1;
  }

  @override
  void initState() {
    super.initState();
    _selectedLessonId = widget.lessonId;
    if (_selectedLessonId != null) {
      _fetchQuestions();
    } else {
      _isLoading = false;
      _fetchChapters();
    }
  }

  Future<void> _fetchChapters() async {
    setState(() => _isLoadingChapters = true);
    try {
      final resp = await ApiClient.instance.get('/admin/chapters');
      final data = resp.data;
      final list = data is List ? data : (data is Map && data['data'] is List ? data['data'] : []);
      _chapters = (list as List).map((e) => ChapterModel.fromJson(e as Map<String, dynamic>)).toList();
    } catch (_) {}
    setState(() => _isLoadingChapters = false);
  }

  Future<void> _fetchLessonsForChapter(String chapterId) async {
    setState(() {
      _chapterLessons = [];
      _selectedLessonId = null;
    });
    try {
      final lessonResp = await ApiClient.instance.get('/admin/lessons/chapter/$chapterId');
      final lData = lessonResp.data;
      final lList = lData is List ? lData : (lData is Map && lData['data'] is List ? lData['data'] : []);
      setState(() {
        _chapterLessons = (lList as List).map((e) => LessonModel.fromJson(e as Map<String, dynamic>)).toList();
      });
    } catch (_) {}
  }

  Future<void> _fetchQuestions() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      if (_selectedLessonId != null) {
        final resp = await ApiClient.instance.get('/admin/lessons/${_selectedLessonId}');
        final data = resp.data;
        Map<String, dynamic> map;
        if (data is Map<String, dynamic>) {
          map = data;
          if (map.containsKey('data') && map['data'] is Map<String, dynamic>) {
            map = map['data'] as Map<String, dynamic>;
          }
        } else {
          throw const FormatException('Invalid response');
        }
        final lesson = LessonModel.fromJson(map);
        _lessonTitle = lesson.title;
        _questions = List.from(lesson.questions);
      } else {
        _lessonTitle = 'Tất cả câu hỏi';
        _questions = [];
      }
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _showQuestionDialog({QuestionModel? question}) async {
    final isEdit = question != null;
    final formKey = GlobalKey<FormState>();
    final questionCtrl = TextEditingController(text: question?.questionText ?? '');
    final optionCtrls = List.generate(4, (i) {
      return TextEditingController(text: (question?.options.length ?? 0) > i ? question!.options[i] : '');
    });
    final explanationCtrl = TextEditingController(text: question?.explanation ?? '');
    int correctOption = question?.correctOption ?? 0;

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) {
          return Dialog(
            insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 48),
            child: ConstrainedBox(
              constraints: BoxConstraints(
                maxWidth: MediaQuery.of(ctx).size.width > 600 ? 600 : MediaQuery.of(ctx).size.width - 48,
              ),
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(isEdit ? 'Sửa câu hỏi' : 'Thêm câu hỏi', style: Theme.of(ctx).textTheme.headlineSmall),
                    const SizedBox(height: 16),
                    Flexible(
                      child: SingleChildScrollView(
                        child: Form(
                          key: formKey,
                          child: Column(
                          mainAxisSize: MainAxisSize.min,
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const SizedBox(height: 4),
                            MathToolbar(
                              onInsert: (latex) {
                                final text = questionCtrl.text;
                                final sel = questionCtrl.selection;
                                final start = sel.isValid ? sel.start : text.length;
                                final end = sel.isValid ? sel.end : text.length;
                                final wrapped = latex.contains(r'\begin') ? r'$$' + latex + r'$$' : r'$' + latex + r'$';
                                final insert = _inMathMode(text, start) ? latex : wrapped;
                                final newText = text.replaceRange(start, end, insert);
                                questionCtrl.value = TextEditingValue(
                                  text: newText,
                                  selection: TextSelection.collapsed(offset: start + insert.length),
                                );
                                setDialogState(() {});
                              },
                            ),
                            const SizedBox(height: 4),
                            TextFormField(
                              controller: questionCtrl,
                              decoration: const InputDecoration(
                                hintText: 'Nhập câu hỏi (hỗ trợ LaTeX)',
                                border: OutlineInputBorder(),
                              ),
                              maxLines: 3,
                              onChanged: (_) => setDialogState(() {}),
                              validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập câu hỏi' : null,
                            ),
                            if (questionCtrl.text.isNotEmpty) ...[
                              const SizedBox(height: 8),
                              Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: Colors.grey.withAlpha(20),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('Xem trước:', style: Theme.of(context).textTheme.labelSmall),
                                    MathText(questionCtrl.text),
                                  ],
                                ),
                              ),
                            ],
                            const SizedBox(height: 16),
                            const Text('Đáp án:', style: TextStyle(fontWeight: FontWeight.w600)),
                            const SizedBox(height: 8),
                            RadioGroup<int>(
                              groupValue: correctOption,
                              onChanged: (v) => setDialogState(() => correctOption = v ?? 0),
                              child: Column(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  for (int i = 0; i < 4; i++)
                                    Padding(
                                      padding: const EdgeInsets.only(bottom: 8),
                                      child: Row(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Radio<int>(value: i),
                                          Expanded(
                                            child: TextFormField(
                                              controller: optionCtrls[i],
                                              decoration: InputDecoration(
                                                labelText: 'Đáp án ${String.fromCharCode(65 + i)}',
                                                border: const OutlineInputBorder(),
                                                isDense: true,
                                              ),
                                              validator: (v) {
                                                if (v == null || v.trim().isEmpty) {
                                                  return 'Vui lòng nhập đáp án ${String.fromCharCode(65 + i)}';
                                                }
                                                return null;
                                              },
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 16),
                            const Text('Giải thích:', style: TextStyle(fontWeight: FontWeight.w600)),
                            const SizedBox(height: 4),
                            MathToolbar(
                              onInsert: (latex) {
                                final text = explanationCtrl.text;
                                final sel = explanationCtrl.selection;
                                final start = sel.isValid ? sel.start : text.length;
                                final end = sel.isValid ? sel.end : text.length;
                                final wrapped = latex.contains(r'\begin') ? r'$$' + latex + r'$$' : r'$' + latex + r'$';
                                final insert = _inMathMode(text, start) ? latex : wrapped;
                                final newText = text.replaceRange(start, end, insert);
                                explanationCtrl.value = TextEditingValue(
                                  text: newText,
                                  selection: TextSelection.collapsed(offset: start + insert.length),
                                );
                                setDialogState(() {});
                              },
                            ),
                            const SizedBox(height: 4),
                            TextFormField(
                              controller: explanationCtrl,
                              decoration: const InputDecoration(
                                labelText: 'Giải thích',
                                border: OutlineInputBorder(),
                              ),
                              maxLines: 3,
                              onChanged: (_) => setDialogState(() {}),
                            ),
                            if (explanationCtrl.text.isNotEmpty) ...[
                              const SizedBox(height: 8),
                              Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: Colors.blue.withAlpha(15),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('Xem trước:', style: Theme.of(context).textTheme.labelSmall),
                                    MathText(explanationCtrl.text),
                                  ],
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    ),   // closes Flexible
                    const SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
                        const SizedBox(width: 8),
                        FilledButton(
                          onPressed: () async {
                            if (!formKey.currentState!.validate()) return;
                            try {
                              final body = {
                                'lessonId': _selectedLessonId,
                                'questionText': questionCtrl.text.trim(),
                                'options': List.generate(4, (i) => optionCtrls[i].text.trim()),
                                'correctOption': correctOption,
                                'explanation': explanationCtrl.text.trim(),
                              };
                              if (isEdit) {
                                await ApiClient.instance.put('/admin/questions/${question!.id}', data: body);
                              } else {
                                await ApiClient.instance.post('/admin/questions', data: body);
                              }
                              if (ctx.mounted) Navigator.pop(ctx, true);
                            } catch (e) {
                              if (ctx.mounted) {
                                ScaffoldMessenger.of(ctx).showSnackBar(
                                  SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
                                );
                              }
                            }
                          },
                          child: Text(isEdit ? 'Cập nhật' : 'Tạo'),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
    if (result == true) _fetchQuestions();
  }

  Future<void> _deleteQuestion(QuestionModel question) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: const Text('Bạn có chắc muốn xóa câu hỏi này?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: Theme.of(ctx).colorScheme.error),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    try {
      await ApiClient.instance.delete('/admin/questions/${question.id}');
      _fetchQuestions();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa câu hỏi')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    String title;

    if (_selectedLessonId == null) {
      title = 'Câu hỏi';
      body = Center(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.help_outline, size: 64, color: Colors.grey),
              const SizedBox(height: 16),
              const Text('Chọn một bài học để xem câu hỏi', style: TextStyle(fontSize: 16)),
              const SizedBox(height: 24),
              _isLoadingChapters
                  ? const CircularProgressIndicator()
                  : _chapters.isEmpty
                      ? const Text('Không có chương nào')
                      : Column(
                          children: [
                            DropdownButtonFormField<String>(
                              isExpanded: true,
                              value: _selectedChapterId,
                              decoration: const InputDecoration(
                                labelText: 'Chọn chương',
                                border: OutlineInputBorder(),
                              ),
                              items: _chapters
                                  .map((c) => DropdownMenuItem(value: c.id, child: Text(c.title)))
                                  .toList(),
                              onChanged: (val) {
                                if (val != null) {
                                  setState(() => _selectedChapterId = val);
                                  _fetchLessonsForChapter(val);
                                }
                              },
                            ),
                            const SizedBox(height: 16),
                            DropdownButtonFormField<String>(
                              isExpanded: true,
                              value: _selectedLessonId,
                              decoration: const InputDecoration(
                                labelText: 'Chọn bài học',
                                border: OutlineInputBorder(),
                              ),
                              items: _chapterLessons
                                  .map((l) => DropdownMenuItem(value: l.id, child: Text(l.title)))
                                  .toList(),
                              onChanged: _selectedChapterId == null
                                  ? null
                                  : (val) {
                                      setState(() => _selectedLessonId = val);
                                      _fetchQuestions();
                                    },
                            ),
                          ],
                        ),
            ],
          ),
        ),
      );
    } else if (_isLoading) {
      title = 'Câu hỏi';
      body = const AppLoadingWidget(message: 'Đang tải câu hỏi...');
    } else if (_error != null) {
      title = 'Câu hỏi';
      body = AppErrorWidget(message: _error!, onRetry: _fetchQuestions);
    } else {
      title = 'Câu hỏi: $_lessonTitle';
      body = RefreshIndicator(
        onRefresh: _fetchQuestions,
        child: _questions.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có câu hỏi nào', style: TextStyle(fontSize: 16))),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: _questions.length,
                itemBuilder: (context, index) {
                  final q = _questions[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              CircleAvatar(
                                radius: 14,
                                backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                                child: Text('${index + 1}', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                              ),
                              const SizedBox(width: 12),
                              Expanded(child: MathText(q.questionText)),
                              PopupMenuButton(
                                itemBuilder: (_) => [
                                  PopupMenuItem(
                                    child: const ListTile(leading: Icon(Icons.edit), title: Text('Sửa')),
                                    onTap: () => _showQuestionDialog(question: q),
                                  ),
                                  PopupMenuItem(
                                    child: const ListTile(leading: Icon(Icons.delete, color: Colors.red), title: Text('Xóa')),
                                    onTap: () => _deleteQuestion(q),
                                  ),
                                ],
                              ),
                            ],
                          ),
                          const Divider(),
                          for (int i = 0; i < q.options.length; i++)
                            Container(
                              margin: const EdgeInsets.only(bottom: 4),
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                              decoration: BoxDecoration(
                                color: q.correctOption == i ? Colors.green.withAlpha(25) : null,
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(
                                  color: q.correctOption == i ? Colors.green : Colors.grey.withAlpha(76),
                                ),
                              ),
                              child: Row(
                                children: [
                                  Container(
                                    width: 24,
                                    height: 24,
                                    alignment: Alignment.center,
                                    decoration: BoxDecoration(
                                      shape: BoxShape.circle,
                                      color: q.correctOption == i ? Colors.green : Colors.grey.withAlpha(51),
                                    ),
                                    child: Text(
                                      String.fromCharCode(65 + i),
                                      style: TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.bold,
                                        color: q.correctOption == i ? Colors.white : null,
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: SingleChildScrollView(
                                      scrollDirection: Axis.horizontal,
                                      child: MathText(q.options[i]),
                                    ),
                                  ),
                                  if (q.correctOption == i)
                                    const Icon(Icons.check_circle, color: Colors.green, size: 20),
                                ],
                              ),
                            ),
                          if (q.explanation.isNotEmpty) ...[
                            const SizedBox(height: 8),
                            Container(
                              padding: const EdgeInsets.all(8),
                              decoration: BoxDecoration(
                                color: Colors.blue.withAlpha(15),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Icon(Icons.lightbulb_outline, size: 16, color: Colors.blue),
                                  const SizedBox(width: 8),
                                  Expanded(
                                    child: SingleChildScrollView(
                                      scrollDirection: Axis.horizontal,
                                      child: MathText(q.explanation, textStyle: const TextStyle(fontSize: 13)),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                  );
                },
              ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        leading: _selectedLessonId != null
            ? IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () => setState(() {
                  _selectedLessonId = null;
                  _questions = [];
                  _lessonTitle = '';
                  // Note: keep _selectedChapterId and _chapterLessons to easily switch lessons within same chapter
                }),
              )
            : null,
        title: Text(title),
        actions: [
          IconButton(
            icon: const Icon(Icons.auto_awesome),
            tooltip: 'Tạo tự động',
            onPressed: () async {
              // We need to import auto_generate_questions_dialog.dart
              final result = await showDialog(
                context: context,
                builder: (ctx) => const math_dialog.AutoGenerateQuestionsDialog(),
              );
              if (result == true && _selectedLessonId != null) {
                _fetchQuestions();
              }
            },
          ),
          if (_selectedLessonId != null && !_isLoading && _error == null)
            IconButton(icon: const Icon(Icons.refresh), onPressed: _fetchQuestions),
        ],
      ),
      body: body,
      floatingActionButton: _selectedLessonId != null && !_isLoading && _error == null
          ? FloatingActionButton(
              onPressed: () => _showQuestionDialog(),
              tooltip: 'Thêm câu hỏi',
              child: const Icon(Icons.add),
            )
          : null,
    );
  }
}
