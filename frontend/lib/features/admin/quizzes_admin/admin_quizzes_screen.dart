import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/models/admin_quiz_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminQuizzesScreen extends StatefulWidget {
  const AdminQuizzesScreen({super.key});

  @override
  State<AdminQuizzesScreen> createState() => _AdminQuizzesScreenState();
}

class _AdminQuizzesScreenState extends State<AdminQuizzesScreen> {
  List<ChapterModel> _chapters = [];
  List<LessonModel> _lessons = [];
  List<AdminQuizModel> _quizzes = [];

  String? _selectedChapterId;
  String? _selectedLessonId;

  bool _isLoading = true;
  String? _error;

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
      final resp = await ApiClient.instance.get('/admin/chapters');
      final data = resp.data;
      final list = _extractList(data);
      _chapters = list
          .map((e) => ChapterModel.fromJson(e as Map<String, dynamic>))
          .toList();

      if (_chapters.isNotEmpty) {
        _selectedChapterId = _chapters.first.id;
        await _fetchLessons();
      }
    } catch (e) {
      _error = e is DioException
          ? ApiClient.mapDioErrorToMessage(e)
          : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _fetchLessons() async {
    if (_selectedChapterId == null) return;
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final resp = await ApiClient.instance.get(
        '/admin/lessons/chapter/$_selectedChapterId',
      );
      final data = resp.data;
      final list = _extractList(data);
      _lessons = list
          .map((e) => LessonModel.fromJson(e as Map<String, dynamic>))
          .toList();

      _selectedLessonId = null;
      await _fetchQuizzes();
    } catch (e) {
      _error = e is DioException
          ? ApiClient.mapDioErrorToMessage(e)
          : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _fetchQuizzes() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      String query = '';
      if (_selectedLessonId != null) {
        query = '?lessonId=$_selectedLessonId';
      } else if (_selectedChapterId != null) {
        query = '?chapterId=$_selectedChapterId';
      }

      final resp = await ApiClient.instance.get('/admin/quizzes$query');
      final data = resp.data;
      final list = _extractList(data);
      _quizzes = list
          .map((e) => AdminQuizModel.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      _error = e is DioException
          ? ApiClient.mapDioErrorToMessage(e)
          : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<dynamic> _extractList(dynamic data) {
    if (data is List) return data;
    if (data is Map<String, dynamic> &&
        data.containsKey('data') &&
        data['data'] is List) {
      return data['data'] as List<dynamic>;
    }
    return [];
  }

  Future<void> _deleteQuiz(AdminQuizModel quiz) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: Text('Bạn có chắc muốn xóa Quiz: ${quiz.title}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Xóa', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );
    if (confirm != true) return;

    try {
      await ApiClient.instance.delete('/admin/quizzes/${quiz.id}');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa Quiz thành công!')),
        );
      }
      _fetchQuizzes();
    } catch (e) {
      if (mounted) {
        final err = e is DioException
            ? ApiClient.mapDioErrorToMessage(e)
            : e.toString();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: $err'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _togglePublish(AdminQuizModel quiz, bool isPublished) async {
    try {
      await ApiClient.instance.patch('/admin/quizzes/${quiz.id}/publish');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              isPublished
                  ? 'Đã kích hoạt Quiz cho học sinh!'
                  : 'Đã hủy kích hoạt Quiz!',
            ),
          ),
        );
      }
      _fetchQuizzes();
    } catch (e) {
      if (mounted) {
        final err = e is DioException
            ? ApiClient.mapDioErrorToMessage(e)
            : e.toString();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: $err'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _showEditDialog(AdminQuizModel quiz) async {
    final formKey = GlobalKey<FormState>();
    final titleCtrl = TextEditingController(text: quiz.title);
    final durationCtrl = TextEditingController(
      text: (quiz.durationSeconds ~/ 60).toString(),
    );
    final passScoreCtrl = TextEditingController(
      text: quiz.passScore.toString(),
    );
    final countCtrl = TextEditingController(
      text: quiz.questionCount.toString(),
    );

    bool isSaving = false;

    final result = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Chỉnh Sửa Quiz'),
          content: Form(
            key: formKey,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: titleCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Tiêu đề Quiz',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'Vui lòng nhập tiêu đề'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: durationCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Thời gian làm bài (Phút)',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null ||
                            v.trim().isEmpty ||
                            int.tryParse(v) == null)
                        ? 'Vui lòng nhập số hợp lệ'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: passScoreCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Điểm qua môn (Ví dụ: 5.0)',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null ||
                            v.trim().isEmpty ||
                            double.tryParse(v) == null)
                        ? 'Vui lòng nhập số hợp lệ'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: countCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Số lượng câu hỏi',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null ||
                            v.trim().isEmpty ||
                            int.tryParse(v) == null)
                        ? 'Vui lòng nhập số hợp lệ'
                        : null,
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Lưu ý: Thay đổi số lượng câu sẽ làm hệ thống bốc ngẫu nhiên lại từ đầu danh sách câu hỏi.',
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.orange,
                      fontStyle: FontStyle.italic,
                    ),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: isSaving ? null : () => Navigator.pop(ctx, false),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              onPressed: isSaving
                  ? null
                  : () async {
                      if (!formKey.currentState!.validate()) return;
                      setDialogState(() => isSaving = true);
                      try {
                        final durationMins = int.parse(durationCtrl.text);
                        final durationSecs = durationMins * 60;

                        await ApiClient.instance.put(
                          '/admin/quizzes/${quiz.id}',
                          data: {
                            'title': titleCtrl.text.trim(),
                            'durationSeconds': durationSecs,
                            'passScore': double.parse(passScoreCtrl.text),
                            'quizType': quiz.quizType,
                            'lessonId': quiz.lessonId,
                            'chapterId': quiz.chapterId,
                            'rewardPolicyId': quiz.rewardPolicyId,
                            'firstPassCoins': quiz.firstPassCoins,
                            'questionCount': int.parse(countCtrl.text),
                          },
                        );
                        if (mounted) Navigator.pop(ctx, true);
                      } catch (e) {
                        setDialogState(() => isSaving = false);
                        if (mounted) {
                          final err = e is DioException
                              ? ApiClient.mapDioErrorToMessage(e)
                              : e.toString();
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text('Lỗi: $err'),
                              backgroundColor: Colors.red,
                              duration: const Duration(seconds: 4),
                            ),
                          );
                        }
                      }
                    },
              child: isSaving
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Lưu thay đổi'),
            ),
          ],
        ),
      ),
    );

    if (result == true) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Cập nhật Quiz thành công!')),
        );
        _fetchQuizzes();
      }
    }
  }

  Future<void> _showGenerateDialog() async {
    if (_selectedChapterId == null && _selectedLessonId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Vui lòng chọn Chương hoặc Bài học trước'),
        ),
      );
      return;
    }

    final formKey = GlobalKey<FormState>();
    final titleCtrl = TextEditingController();
    final countCtrl = TextEditingController(text: '10');
    final durationCtrl = TextEditingController(text: '20');
    final passScoreCtrl = TextEditingController(text: '5.0');

    bool isGenerating = false;

    final result = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Tạo Quiz Tự Động'),
          content: Form(
            key: formKey,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(
                    'Hệ thống sẽ bốc ngẫu nhiên câu hỏi từ ngân hàng câu hỏi của bài/chương đã chọn.',
                    style: TextStyle(fontSize: 13, color: Colors.grey),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: titleCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Tiêu đề Quiz (Bỏ trống tự động sinh)',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: countCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Số lượng câu hỏi',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null ||
                            v.trim().isEmpty ||
                            int.tryParse(v) == null)
                        ? 'Vui lòng nhập số hợp lệ'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: durationCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Thời gian làm bài (Phút)',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null ||
                            v.trim().isEmpty ||
                            int.tryParse(v) == null)
                        ? 'Vui lòng nhập số hợp lệ'
                        : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: passScoreCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Điểm qua môn (Ví dụ: 5.0)',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null ||
                            v.trim().isEmpty ||
                            double.tryParse(v) == null)
                        ? 'Vui lòng nhập số hợp lệ'
                        : null,
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: isGenerating ? null : () => Navigator.pop(ctx, false),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              onPressed: isGenerating
                  ? null
                  : () async {
                      if (!formKey.currentState!.validate()) return;
                      setDialogState(() => isGenerating = true);
                      try {
                        final durationMins = int.parse(durationCtrl.text);
                        final durationSecs = durationMins * 60;

                        await ApiClient.instance.post(
                          '/admin/quizzes/generate',
                          data: {
                            'title': titleCtrl.text.trim(),
                            'lessonId': _selectedLessonId,
                            'chapterId': _selectedLessonId == null
                                ? _selectedChapterId
                                : null,
                            'questionCount': int.parse(countCtrl.text),
                            'durationSeconds': durationSecs,
                            'passScore': double.parse(passScoreCtrl.text),
                          },
                        );
                        if (mounted) Navigator.pop(ctx, true);
                      } catch (e) {
                        setDialogState(() => isGenerating = false);
                        if (mounted) {
                          final err = e is DioException
                              ? ApiClient.mapDioErrorToMessage(e)
                              : e.toString();
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text('Lỗi: $err'),
                              backgroundColor: Colors.red,
                              duration: const Duration(seconds: 4),
                            ),
                          );
                        }
                      }
                    },
              child: isGenerating
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Tạo Quiz'),
            ),
          ],
        ),
      ),
    );

    if (result == true) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Tạo Quiz tự động thành công!')),
        );
        _fetchQuizzes();
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading && _chapters.isEmpty) {
      return const AppLoadingWidget();
    }
    if (_error != null && _chapters.isEmpty) {
      return AppErrorWidget(message: _error!, onRetry: _fetchChapters);
    }

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // CUSTOM HEADER
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Theme.of(
                      context,
                    ).colorScheme.primary.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(
                    Icons.quiz,
                    size: 28,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Quản lý Bài Kiểm Tra (Quiz)',
                        style: Theme.of(context).textTheme.headlineSmall
                            ?.copyWith(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Tạo, chỉnh sửa và quản lý các bài kiểm tra trắc nghiệm cho học sinh',
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),

            // CASCADING DROPDOWNS
            Card(
              elevation: 2,
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final fieldWidth = constraints.maxWidth.clamp(0.0, 250.0);
                    return Wrap(
                      spacing: 16.0,
                      runSpacing: 16.0,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        SizedBox(
                          width: fieldWidth,
                          child: DropdownButtonFormField<String>(
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Chọn Chương',
                              border: OutlineInputBorder(),
                            ),
                            value: _selectedChapterId,
                            items: _chapters
                                .map(
                                  (c) => DropdownMenuItem(
                                    value: c.id,
                                    child: Text(
                                      c.title,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                                )
                                .toList(),
                            onChanged: (val) {
                              setState(() {
                                _selectedChapterId = val;
                              });
                              _fetchLessons();
                            },
                          ),
                        ),
                        SizedBox(
                          width: fieldWidth,
                          child: DropdownButtonFormField<String>(
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Chọn Bài Học (Không bắt buộc)',
                              border: OutlineInputBorder(),
                            ),
                            value: _selectedLessonId,
                            items: [
                              const DropdownMenuItem(
                                value: null,
                                child: Text('-- Tất cả bài học --'),
                              ),
                              ..._lessons.map(
                                (l) => DropdownMenuItem(
                                  value: l.id,
                                  child: Text(
                                    l.title,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                            ],
                            onChanged: (val) {
                              setState(() {
                                _selectedLessonId = val;
                              });
                              _fetchQuizzes();
                            },
                          ),
                        ),
                        ElevatedButton.icon(
                          onPressed: _showGenerateDialog,
                          icon: const Icon(Icons.auto_awesome),
                          label: const Text('Tạo Quiz Tự Động'),
                          style: ElevatedButton.styleFrom(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 24,
                              vertical: 18,
                            ),
                          ),
                        ),
                      ],
                    );
                  },
                ),
              ),
            ),
            const SizedBox(height: 16),
            Expanded(
              child: _isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : RefreshIndicator(
                      onRefresh: () async => _fetchQuizzes(),
                      child: _quizzes.isEmpty
                          ? ListView(
                              children: const [
                                SizedBox(height: 80),
                                Center(
                                  child: Text(
                                    'Chưa có Quiz nào cho phạm vi đã chọn.',
                                    style: TextStyle(fontSize: 16),
                                  ),
                                ),
                              ],
                            )
                          : ListView.builder(
                              itemCount: _quizzes.length,
                              itemBuilder: (context, index) {
                                final quiz = _quizzes[index];
                                final scopeText = quiz.quizType == 1
                                    ? 'Bài Học'
                                    : 'Chương';
                                final durationMins = quiz.durationSeconds ~/ 60;

                                return Card(
                                  margin: const EdgeInsets.only(bottom: 8),
                                  child: ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: Theme.of(
                                        context,
                                      ).colorScheme.primary.withOpacity(0.1),
                                      child: Icon(
                                        Icons.quiz,
                                        color: Theme.of(
                                          context,
                                        ).colorScheme.primary,
                                      ),
                                    ),
                                    title: Text(
                                      quiz.title,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                    subtitle: Text(
                                      'Phạm vi: $scopeText\nThời gian: $durationMins phút • Số câu: ${quiz.questionCount} • Điểm qua: ${quiz.passScore}',
                                    ),
                                    isThreeLine: true,
                                    trailing: Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Switch(
                                          value: quiz.isPublished,
                                          activeColor: Theme.of(
                                            context,
                                          ).colorScheme.primary,
                                          onChanged: (val) =>
                                              _togglePublish(quiz, val),
                                        ),
                                        PopupMenuButton<String>(
                                          onSelected: (value) {
                                            if (value == 'edit')
                                              _showEditDialog(quiz);
                                            if (value == 'delete')
                                              _deleteQuiz(quiz);
                                          },
                                          itemBuilder: (context) => [
                                            const PopupMenuItem(
                                              value: 'edit',
                                              child: Row(
                                                children: [
                                                  Icon(
                                                    Icons.edit,
                                                    size: 20,
                                                    color: Colors.blue,
                                                  ),
                                                  SizedBox(width: 8),
                                                  Text('Sửa'),
                                                ],
                                              ),
                                            ),
                                            const PopupMenuItem(
                                              value: 'delete',
                                              child: Row(
                                                children: [
                                                  Icon(
                                                    Icons.delete,
                                                    size: 20,
                                                    color: Colors.red,
                                                  ),
                                                  SizedBox(width: 8),
                                                  Text('Xóa'),
                                                ],
                                              ),
                                            ),
                                          ],
                                        ),
                                      ],
                                    ),
                                  ),
                                );
                              },
                            ),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
