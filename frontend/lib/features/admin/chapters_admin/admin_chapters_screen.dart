import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
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
  List<CurriculumTopicDto> _topics = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchAll();
  }

  Future<void> _fetchAll() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      await Future.wait([_fetchTopics(), _fetchChaptersInternal()]);
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _fetchTopics() async {
    try {
      final response = await ApiClient.instance.get('/admin/curriculum-topics');
      final data = response.data;
      final list = data is List ? data : (data is Map && data['data'] is List ? data['data'] : []);
      _topics = (list as List)
          .map((e) => CurriculumTopicDto.fromJson(e as Map<String, dynamic>))
          .where((t) => t.isActive)
          .toList();
    } catch (_) {
      // Topics load failure is non-critical
    }
  }

  Future<void> _fetchChaptersInternal() async {
    final response = await ApiClient.instance.get('/admin/chapters');
    final data = response.data;
    final list = _extractList(data);
    _chapters = list.map((e) => ChapterModel.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> _fetchChapters() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      await _fetchChaptersInternal();
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
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: Text(isEdit ? 'Sửa chương' : 'Thêm chương mới'),
          content: Form(
            key: formKey,
            child: SizedBox(
              width: 480,
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    TextFormField(
                      controller: titleCtrl,
                      decoration: const InputDecoration(labelText: 'Tiêu đề *', border: OutlineInputBorder()),
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
                    if (_topics.isEmpty)
                      const Text(
                        'Không tải được taxonomy. Vui lòng thêm Curriculum Topic trước.',
                        style: TextStyle(color: Colors.orange, fontSize: 12),
                      )
                    else
                      DropdownButtonFormField<String>(
                        value: selectedTopicId,
                        decoration: const InputDecoration(
                          labelText: 'Taxonomy Toán 8 *',
                          border: OutlineInputBorder(),
                        ),
                        items: _topics
                            .map((t) => DropdownMenuItem(value: t.id, child: Text(t.displayName, overflow: TextOverflow.ellipsis)))
                            .toList(),
                        onChanged: (v) => setDialogState(() => selectedTopicId = v),
                        validator: (v) => v == null ? 'Vui lòng chọn taxonomy' : null,
                      ),
                  ],
                ),
              ),
            ),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
            FilledButton(
              onPressed: () async {
                if (!formKey.currentState!.validate()) return;
                if (selectedTopicId == null) {
                  ScaffoldMessenger.of(ctx).showSnackBar(
                    const SnackBar(content: Text('Vui lòng chọn taxonomy Toán 8')),
                  );
                  return;
                }
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
      ),
    );
    if (result == true) _fetchChapters();
  }

  Future<void> _togglePublish(ChapterModel chapter) async {
    try {
      await ApiClient.instance.patch('/admin/chapters/${chapter.id}/publish', data: {});
      _fetchChapters();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(chapter.isPublished ? 'Đã ẩn chương "${chapter.title}"' : 'Đã xuất bản chương "${chapter.title}"'),
          ),
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

  Future<void> _deleteChapter(ChapterModel chapter) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: Text('Bạn có chắc muốn xóa chương "${chapter.title}"?\n\nLưu ý: Chương đã có tiến độ học sẽ bị ẩn (soft delete).'),
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
      await ApiClient.instance.delete('/admin/chapters/${chapter.id}');
      _fetchChapters();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa chương')),
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
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải chương...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchAll);
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
                        backgroundColor: chapter.isPublished
                            ? Theme.of(context).colorScheme.primaryContainer
                            : Colors.grey.withAlpha(40),
                        child: Text(
                          '${chapter.orderIndex}',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ),
                      title: Row(
                        children: [
                          Expanded(
                            child: Text(chapter.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                          ),
                          const SizedBox(width: 8),
                          _buildStatusBadge(chapter.isPublished),
                        ],
                      ),
                      subtitle: chapter.description.isNotEmpty ? Text(chapter.description, maxLines: 2) : null,
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          IconButton(
                            icon: const Icon(Icons.menu_book),
                            tooltip: 'Xem bài học',
                            onPressed: () => context.go('/admin/chapters/${chapter.id}/lessons'),
                          ),
                          IconButton(
                            icon: Icon(
                              chapter.isPublished ? Icons.visibility_off : Icons.visibility,
                              color: chapter.isPublished ? Colors.orange : Colors.green,
                            ),
                            tooltip: chapter.isPublished ? 'Ẩn chương' : 'Xuất bản',
                            onPressed: () => _togglePublish(chapter),
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
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showChapterDialog(),
        icon: const Icon(Icons.add),
        label: const Text('Thêm chương'),
      ),
    );
  }

  Widget _buildStatusBadge(bool isPublished) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: isPublished ? Colors.green.withAlpha(25) : Colors.grey.withAlpha(25),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isPublished ? Colors.green : Colors.grey,
          width: 0.5,
        ),
      ),
      child: Text(
        isPublished ? 'Published' : 'Draft',
        style: TextStyle(
          fontSize: 11,
          color: isPublished ? Colors.green.shade700 : Colors.grey.shade600,
          fontWeight: FontWeight.w500,
        ),
      ),
    );
  }
}
