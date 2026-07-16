import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminChaptersScreen extends StatefulWidget {
  const AdminChaptersScreen({super.key});

  @override
  State<AdminChaptersScreen> createState() => _AdminChaptersScreenState();
}

class _AdminChaptersScreenState extends State<AdminChaptersScreen> {
  List<ChapterModel> _chapters = [];
  List<dynamic> _topics = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchTopics();
    _fetchChapters();
  }

  Future<void> _fetchTopics() async {
    try {
      final response = await ApiClient.instance.get('/admin/curriculum-topics');
      final data = response.data;
      if (data is List) {
        if (mounted) setState(() => _topics = data);
      } else if (data is Map && data['data'] is List) {
        if (mounted) setState(() => _topics = data['data']);
      }
    } catch (_) {}
  }

  Future<void> _fetchChapters() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/admin/chapters');
      final data = response.data;
      final list = _extractList(data);
      _chapters = list.map((e) => ChapterModel.fromJson(e as Map<String, dynamic>)).toList();
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<dynamic> _extractList(dynamic data) {
    if (data is List) return data;
    if (data is Map<String, dynamic> && data.containsKey('data') && data['data'] is List) {
      return data['data'] as List<dynamic>;
    }
    return [];
  }

  Future<void> _showChapterDialog({ChapterModel? chapter}) async {
    final titleCtrl = TextEditingController(text: chapter?.title ?? '');
    final descCtrl = TextEditingController(text: chapter?.description ?? '');
    final orderCtrl = TextEditingController(text: (chapter?.orderIndex ?? 0).toString());
    String? selectedTopicId = chapter?.curriculumTopicId;
    final isEdit = chapter != null;
    final formKey = GlobalKey<FormState>();

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isEdit ? 'Sửa chương' : 'Thêm chương mới'),
        content: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: titleCtrl,
                  decoration: const InputDecoration(labelText: 'Tiêu đề', border: OutlineInputBorder()),
                  validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tiêu đề' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: descCtrl,
                  decoration: const InputDecoration(labelText: 'Mô tả', border: OutlineInputBorder()),
                  maxLines: 3,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: orderCtrl,
                  decoration: const InputDecoration(labelText: 'Thứ tự', border: OutlineInputBorder()),
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  decoration: const InputDecoration(labelText: 'Chủ đề Toán học (Taxonomy)', border: OutlineInputBorder()),
                  value: selectedTopicId,
                  items: _topics.map((t) => DropdownMenuItem<String>(
                    value: t['id'],
                    child: Text(t['name'] ?? 'Unknown', overflow: TextOverflow.ellipsis),
                  )).toList(),
                  onChanged: (v) => setState(() => selectedTopicId = v),
                  validator: (v) => v == null ? 'Vui lòng chọn chủ đề' : null,
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          FilledButton(
            onPressed: () async {
              if (!formKey.currentState!.validate()) return;
              try {
                final body = {
                  'title': titleCtrl.text.trim(),
                  'description': descCtrl.text.trim(),
                  'orderIndex': int.tryParse(orderCtrl.text) ?? 0,
                  'curriculumTopicId': selectedTopicId,
                };
                if (isEdit) {
                  await ApiClient.instance.put('/admin/chapters/${chapter!.id}', data: body);
                } else {
                  await ApiClient.instance.post('/admin/chapters', data: body);
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
    );
    if (result == true) _fetchChapters();
  }

  Future<void> _deleteChapter(ChapterModel chapter) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: Text('Bạn có chắc muốn xóa chương "${chapter.title}" không?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      try {
        await ApiClient.instance.delete('/admin/chapters/${chapter.id}');
        _fetchChapters();
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Lỗi xóa chương: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e}')),
          );
        }
      }
    }
  }

  Future<void> _togglePublish(ChapterModel chapter) async {
    try {
      await ApiClient.instance.patch('/admin/chapters/${chapter.id}/publish');
      _fetchChapters();
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
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải chương...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchChapters);
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _fetchChapters,
        child: _chapters.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có chương nào', style: TextStyle(fontSize: 16))),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: _chapters.length,
                itemBuilder: (context, index) {
                  final chapter = _chapters[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: CircleAvatar(
                        backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                        child: Text('${chapter.orderIndex}', style: const TextStyle(fontWeight: FontWeight.bold)),
                      ),
                      title: Text(chapter.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                      subtitle: chapter.description.isNotEmpty ? Text(chapter.description, maxLines: 2) : null,
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Switch(
                            value: chapter.isPublished,
                            onChanged: (_) => _togglePublish(chapter),
                          ),
                          IconButton(
                            icon: const Icon(Icons.menu_book),
                            tooltip: 'Xem bài học',
                            onPressed: () => context.go('/admin/chapters/${chapter.id}/lessons'),
                          ),
                          IconButton(
                            icon: const Icon(Icons.edit),
                            tooltip: 'Sửa',
                            onPressed: () => _showChapterDialog(chapter: chapter),
                          ),
                          IconButton(
                            icon: const Icon(Icons.delete, color: Colors.red),
                            tooltip: 'Xóa',
                            onPressed: () => _deleteChapter(chapter),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showChapterDialog(),
        tooltip: 'Thêm chương',
        child: const Icon(Icons.add),
      ),
    );
  }
}
