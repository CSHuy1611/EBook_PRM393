import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:math_ibook/core/models/notification_model.dart';
import 'package:math_ibook/core/network/api_client.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<NotificationDto> _notifications = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetch();
  }

  Future<void> _fetch() async {
    setState(() { _isLoading = true; _error = null; });
    try {
      final res = await ApiClient.instance.get('/notifications');
      final list = (res.data as List).map((e) => NotificationDto.fromJson(e)).toList();
      setState(() { _notifications = list; _isLoading = false; });
    } catch (e) {
      setState(() { _error = e.toString(); _isLoading = false; });
    }
  }

  Future<void> _markRead(String id) async {
    try {
      await ApiClient.instance.put('/notifications/$id/read');
      setState(() {
        final idx = _notifications.indexWhere((n) => n.id == id);
        if (idx >= 0) _notifications[idx] = NotificationDto(
          id: _notifications[idx].id,
          title: _notifications[idx].title,
          body: _notifications[idx].body,
          link: _notifications[idx].link,
          type: _notifications[idx].type,
          relatedEntityId: _notifications[idx].relatedEntityId,
          isRead: true,
          createdAt: _notifications[idx].createdAt,
        );
      });
    } catch (_) {}
  }

  String _timeAgo(DateTime dt) {
    final diff = DateTime.now().toUtc().difference(dt.toUtc());
    if (diff.inMinutes < 1) return 'Vừa xong';
    if (diff.inMinutes < 60) return '${diff.inMinutes} phút trước';
    if (diff.inHours < 24) return '${diff.inHours} giờ trước';
    if (diff.inDays < 7) return '${diff.inDays} ngày trước';
    return '${dt.day}/${dt.month}/${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Thông báo')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text(_error!))
              : _notifications.isEmpty
                  ? const Center(child: Text('Không có thông báo nào'))
                  : RefreshIndicator(
                      onRefresh: _fetch,
                      child: ListView.separated(
                        padding: const EdgeInsets.all(8),
                        itemCount: _notifications.length,
                        separatorBuilder: (_, __) => const Divider(height: 1),
                        itemBuilder: (ctx, i) {
                          final n = _notifications[i];
                          return ListTile(
                            leading: Icon(
                              n.isRead ? Icons.notifications_none : Icons.notifications,
                              color: n.isRead ? Colors.grey : Theme.of(context).colorScheme.primary,
                            ),
                            title: Text(n.title, style: TextStyle(fontWeight: n.isRead ? FontWeight.normal : FontWeight.bold)),
                            subtitle: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                if (n.body.isNotEmpty) Text(n.body, maxLines: 2, overflow: TextOverflow.ellipsis),
                                Text(_timeAgo(n.createdAt), style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
                              ],
                            ),
                            onTap: () {
                              if (!n.isRead) _markRead(n.id);
                              if (n.link != null && n.link!.isNotEmpty) context.push(n.link!);
                            },
                          );
                        },
                      ),
                    ),
    );
  }
}

