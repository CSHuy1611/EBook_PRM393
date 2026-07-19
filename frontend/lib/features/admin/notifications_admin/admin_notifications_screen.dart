import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/notification_model.dart';
import 'package:math_ibook/core/models/admin_models.dart';
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
    final userIdCtrl = TextEditingController(); // Stores selected ID
    final searchCtrl = TextEditingController(); // Search query
    String type = 'admin_message';
    AdminUserDto? selectedUser;
    
    List<AdminUserDto> users = [];
    bool isLoadingUsers = true;

    // Fetch users async
    ApiClient.instance.get('/admin/users').then((response) {
      if (!mounted) return;
      final data = response.data;
      var list = <dynamic>[];
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['data'] is List) {
        list = data['data'] as List<dynamic>;
      }
      users = list.map((e) => AdminUserDto.fromJson(e)).toList();
      isLoadingUsers = false;
    }).catchError((_) {
      isLoadingUsers = false;
    });

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Gửi thông báo mới'),
          content: Form(
            key: formKey,
            child: ConstrainedBox(
              constraints: BoxConstraints(
                maxWidth: MediaQuery.of(ctx).size.width > 600 ? 500 : MediaQuery.of(ctx).size.width - 48,
              ),
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
                    Autocomplete<AdminUserDto>(
                      optionsBuilder: (TextEditingValue v) {
                        if (v.text.isEmpty) return const Iterable<AdminUserDto>.empty();
                        return users.where((u) =>
                            u.name.toLowerCase().contains(v.text.toLowerCase()) ||
                            u.email.toLowerCase().contains(v.text.toLowerCase()));
                      },
                      displayStringForOption: (u) => '${u.name} (${u.email})',
                      onSelected: (u) {
                        selectedUser = u;
                        userIdCtrl.text = u.id;
                      },
                      fieldViewBuilder: (ctx, ctrl, focus, submit) {
                        return TextFormField(
                          controller: ctrl,
                          focusNode: focus,
                          decoration: InputDecoration(
                            labelText: 'Gửi cho cá nhân (Nhập tên/email để tìm, bỏ trống = TẤT CẢ)',
                            border: const OutlineInputBorder(),
                            suffixIcon: ctrl.text.isNotEmpty
                                ? IconButton(
                                    icon: const Icon(Icons.clear),
                                    onPressed: () {
                                      ctrl.clear();
                                      selectedUser = null;
                                      userIdCtrl.clear();
                                    },
                                  )
                                : null,
                          ),
                          onChanged: (v) {
                            if (v.isEmpty) {
                              selectedUser = null;
                              userIdCtrl.clear();
                            }
                          },
                        );
                      },
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

  IconData _getIconForType(String type) {
    switch (type.toLowerCase()) {
      case 'system': return Icons.info;
      case 'reward': return Icons.monetization_on;
      case 'badge': return Icons.workspace_premium;
      case 'quiz': return Icons.quiz;
      default: return Icons.notifications;
    }
  }

  Color _getColorForType(String type) {
     switch (type.toLowerCase()) {
      case 'system': return Colors.blue;
      case 'reward': return Colors.orange;
      case 'badge': return Colors.purple;
      case 'quiz': return Colors.green;
      default: return Colors.grey;
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
                        backgroundColor: _getColorForType(notif.type).withAlpha(50),
                        child: Icon(_getIconForType(notif.type), color: _getColorForType(notif.type)),
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
