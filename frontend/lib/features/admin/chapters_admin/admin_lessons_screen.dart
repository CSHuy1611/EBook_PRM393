import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/features/admin/lessons_admin/lesson_editor_screen.dart';

class AdminLessonsScreen extends StatefulWidget {
  final String? chapterId;
  const AdminLessonsScreen({super.key, this.chapterId});

  @override
  State<AdminLessonsScreen> createState() => _AdminLessonsScreenState();
}

class _AdminLessonsScreenState extends State<AdminLessonsScreen> {
  List<LessonModel> _lessons = [];
  List<ChapterModel> _chapters = [];
  String? _selectedChapterId;
  bool _isLoading = true;
  bool _isLoadingChapters = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _selectedChapterId = widget.chapterId;
    if (_selectedChapterId != null) {
      _fetchLessons();
    } else {
      _fetchChaptersThenLessons();
    }
  }

  Future<void> _fetchChaptersThenLessons() async {
    setState(() {
      _isLoading = true;
      _isLoadingChapters = true;
      _error = null;
    });
    try {
      final resp = await ApiClient.instance.get('/admin/chapters');
      final data = resp.data;
      final list = _extractList(data);
      _chapters = list.map((e) => ChapterModel.fromJson(e as Map<String, dynamic>)).toList();
      if (_chapters.isNotEmpty && _selectedChapterId == null) {
        _selectedChapterId = _chapters.first.id;
      }
      if (_selectedChapterId != null) {
        await _fetchLessonsInternal();
      } else {
        _lessons = [];
      }
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() {
        _isLoading = false;
        _isLoadingChapters = false;
      });
    }
  }

  Future<void> _fetchLessons() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      await _fetchLessonsInternal();
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _fetchLessonsInternal() async {
    if (_selectedChapterId == null) return;
    final response = await ApiClient.instance.get('/admin/lessons/chapter/$_selectedChapterId');
    final data = response.data;
    final list = _extractList(data);
    _lessons = list.map((e) => LessonModel.fromJson(e as Map<String, dynamic>)).toList();
  }

  List<dynamic> _extractList(dynamic data) {
    if (data is List) return data;
    if (data is Map<String, dynamic> && data.containsKey('data') && data['data'] is List) {
      return data['data'] as List<dynamic>;
    }
    return [];
  }

  Future<void> _togglePublish(LessonModel lesson) async {
    try {
      await ApiClient.instance.patch('/admin/lessons/${lesson.id}/publish', data: {
        'isPublished': !lesson.isPublished,
      });
      _fetchLessons();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
        );
      }
    }
  }

  Future<void> _deleteLesson(LessonModel lesson) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: Text('Bạn có chắc muốn xóa bài học "${lesson.title}"?'),
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
      await ApiClient.instance.delete('/admin/lessons/${lesson.id}');
      _fetchLessons();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa bài học')),
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

  void _openEditor({LessonModel? lesson}) async {
    if (_selectedChapterId == null) return;
    final result = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => LessonEditorScreen(
          chapterId: _selectedChapterId!,
          lesson: lesson,
        ),
      ),
    );
    if (result == true) _fetchLessons();
  }

  void _viewContent(LessonModel lesson) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(lesson.title),
        content: SizedBox(
          width: double.maxFinite,
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                if (lesson.contentBody.isNotEmpty) ...[
                  const Text('Nội dung:', style: TextStyle(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  MathText(lesson.contentBody),
                ],
                if (lesson.simulationType != null && lesson.simulationType!.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text('Mô phỏng: ${lesson.simulationType}',
                      style: Theme.of(context).textTheme.bodySmall),
                ],
              ],
            ),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Đóng')),
          FilledButton.icon(
            icon: const Icon(Icons.help_outline),
            label: const Text('Câu hỏi'),
            onPressed: () {
              Navigator.pop(ctx);
              context.go('/admin/lessons/${lesson.id}/questions');
            },
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải bài học...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchLessons);
    return Scaffold(
      body: Column(
        children: [
          if (widget.chapterId == null) _buildChapterSelector(),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _fetchLessons,
              child: _lessons.isEmpty
                  ? ListView(
                      children: const [
                        SizedBox(height: 120),
                        Center(child: Text('Chưa có bài học nào', style: TextStyle(fontSize: 16))),
                      ],
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(16),
                      itemCount: _lessons.length,
                      itemBuilder: (context, index) {
                        final lesson = _lessons[index];
                        return Card(
                          margin: const EdgeInsets.only(bottom: 8),
                          child: ListTile(
                            leading: CircleAvatar(
                              backgroundColor: lesson.isPublished
                                  ? Colors.green.withAlpha(25)
                                  : Colors.grey.withAlpha(25),
                              child: Icon(
                                lesson.isPublished ? Icons.check_circle : Icons.unpublished_outlined,
                                color: lesson.isPublished ? Colors.green : Colors.grey,
                              ),
                            ),
                            title: Text(lesson.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('Thứ tự: ${lesson.orderIndex}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Switch(
                                  value: lesson.isPublished,
                                  onChanged: (_) => _togglePublish(lesson),
                                ),
                                PopupMenuButton<String>(
                                  onSelected: (value) {
                                    if (value == 'view') _viewContent(lesson);
                                    if (value == 'questions') context.go('/admin/lessons/${lesson.id}/questions');
                                    if (value == 'edit') _openEditor(lesson: lesson);
                                    if (value == 'delete') _deleteLesson(lesson);
                                  },
                                  itemBuilder: (context) => [
                                    const PopupMenuItem(
                                      value: 'view',
                                      child: Row(children: [Icon(Icons.visibility, size: 20), SizedBox(width: 8), Text('Xem nội dung')]),
                                    ),
                                    const PopupMenuItem(
                                      value: 'questions',
                                      child: Row(children: [Icon(Icons.help_outline, size: 20), SizedBox(width: 8), Text('Câu hỏi')]),
                                    ),
                                    const PopupMenuItem(
                                      value: 'edit',
                                      child: Row(children: [Icon(Icons.edit, size: 20), SizedBox(width: 8), Text('Sửa')]),
                                    ),
                                    const PopupMenuItem(
                                      value: 'delete',
                                      child: Row(children: [Icon(Icons.delete, size: 20, color: Colors.red), SizedBox(width: 8), Text('Xóa')]),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                            onTap: () => _viewContent(lesson),
                          ),
                        );
                      },
                    ),
            ),
          ),
        ],
      ),
      floatingActionButton: _selectedChapterId != null
          ? FloatingActionButton(
              onPressed: () => _openEditor(),
              tooltip: 'Thêm bài học',
              child: const Icon(Icons.add),
            )
          : null,
    );
  }

  Widget _buildChapterSelector() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
      child: DropdownButtonFormField<String>(
        isExpanded: true,
        value: _selectedChapterId,
        decoration: const InputDecoration(
          labelText: 'Chọn chương',
          border: OutlineInputBorder(),
          contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
        ),
        items: _chapters.map((c) => DropdownMenuItem(value: c.id, child: Text(c.title))).toList(),
        onChanged: (val) {
          setState(() => _selectedChapterId = val);
          _fetchLessons();
        },
      ),
    );
  }
}
