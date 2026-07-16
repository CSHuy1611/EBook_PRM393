import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/notification_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminNotificationsScreen extends StatefulWidget {
  const AdminNotificationsScreen({super.key});

  @override
  State<AdminNotificationsScreen> createState() => _AdminNotificationsScreenState();
}

class _AdminNotificationsScreenState extends State<AdminNotificationsScreen> {
  List<NotificationDto> _notifications = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchNotifications();
  }

  Future<void> _fetchNotifications() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/admin/notifications');
      final data = response.data;
      final list = _extractList(data);
      _notifications = list.map((e) => NotificationDto.fromJson(e as Map<String, dynamic>)).toList();
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

  Future<void> _showCreateDialog() async {
    final formKey = GlobalKey<FormState>();
    final titleCtrl = TextEditingController();
    final bodyCtrl = TextEditingController();
    final linkCtrl = TextEditingController();
    final userIdCtrl = TextEditingController(); // Empty for all students
    String type = 'admin_message';

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Gửi thông báo mới'),
          content: Form(
            key: formKey,
            child: SizedBox(
              width: 500,
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    TextFormField(
                      controller: titleCtrl,
                      decoration: const InputDecoration(labelText: 'Tiêu đề *', border: OutlineInputBorder()),
                      validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tiêu đề' : null,
                      maxLength: 200,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: bodyCtrl,
                      decoration: const InputDecoration(labelText: 'Nội dung *', border: OutlineInputBorder()),
                      validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập nội dung' : null,
                      maxLines: 4,
                      maxLength: 1000,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: linkCtrl,
                      decoration: const InputDecoration(labelText: 'Liên kết đích (Tùy chọn)', border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<String>(
                      value: type,
                      decoration: const InputDecoration(labelText: 'Loại thông báo *', border: OutlineInputBorder()),
                      items: const [
                        DropdownMenuItem(value: 'admin_message', child: Text('Tin nhắn Admin')),
                        DropdownMenuItem(value: 'new_chapter', child: Text('Chương/Bài mới')),
                        DropdownMenuItem(value: 'quiz_opened', child: Text('Quiz được mở')),
                        DropdownMenuItem(value: 'badge_awarded', child: Text('Nhận huy hiệu')),
                      ],
                      onChanged: (v) {
                        if (v != null) setDialogState(() => type = v);
                      },
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: userIdCtrl,
                      decoration: const InputDecoration(
                        labelText: 'ID học sinh (Bỏ trống để gửi toàn bộ)',
                        border: OutlineInputBorder(),
                      ),
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
                try {
                  final reqBody = {
                    'title': titleCtrl.text.trim(),
                    'body': bodyCtrl.text.trim(),
                    'link': linkCtrl.text.trim().isEmpty ? null : linkCtrl.text.trim(),
                    'type': type,
                    'userId': userIdCtrl.text.trim().isEmpty ? null : userIdCtrl.text.trim(),
                  };
                  await ApiClient.instance.post('/admin/notifications', data: reqBody);
                  if (ctx.mounted) Navigator.pop(ctx, true);
                } catch (e) {
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
                    );
                  }
                }
              },
              child: const Text('Gửi'),
            ),
          ],
        ),
      ),
    );
    if (result == true) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã gửi thông báo thành công')),
        );
      }
      _fetchNotifications();
    }
  }

  Future<void> _deleteNotification(NotificationDto notif) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: const Text('Bạn có chắc muốn xóa thông báo này?'),
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
      await ApiClient.instance.delete('/admin/notifications/${notif.id}');
      _fetchNotifications();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa thông báo')),
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
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải thông báo...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchNotifications);

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _fetchNotifications,
        child: _notifications.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có thông báo nào được gửi', style: TextStyle(fontSize: 16))),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: _notifications.length,
                itemBuilder: (context, index) {
                  final notif = _notifications[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: CircleAvatar(
                        backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                        child: Icon(Icons.notifications, color: Theme.of(context).colorScheme.primary),
                      ),
                      title: Text(notif.title, style: const TextStyle(fontWeight: FontWeight.bold)),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(notif.body, maxLines: 2, overflow: TextOverflow.ellipsis),
                          const SizedBox(height: 4),
                          Text(
                            'Loại: ${notif.type} • Ngày: ${notif.createdAt.toString().split('.')[0]}',
                            style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
                          ),
                        ],
                      ),
                      trailing: IconButton(
                        icon: const Icon(Icons.delete, color: Colors.red),
                        tooltip: 'Xóa',
                        onPressed: () => _deleteNotification(notif),
                      ),
                    ),
                  );
                },
              ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _showCreateDialog,
        icon: const Icon(Icons.send),
        label: const Text('Gửi thông báo'),
      ),
    );
  }
}
