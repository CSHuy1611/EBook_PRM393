import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/badge_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminBadgesScreen extends StatefulWidget {
  const AdminBadgesScreen({super.key});

  @override
  State<AdminBadgesScreen> createState() => _AdminBadgesScreenState();
}

class _AdminBadgesScreenState extends State<AdminBadgesScreen> {
  List<BadgeDto> _badges = [];
  bool _isLoading = true;
  String? _error;

  static const _conditionTypes = [
    'complete_chapter',
    'complete_book',
    'perfect_quiz_streak',
    'total_coins',
  ];

  static const _conditionLabels = {
    'complete_chapter': 'Hoàn thành chương',
    'complete_book': 'Hoàn thành sách',
    'perfect_quiz_streak': 'Quiz hoàn hảo liên tiếp',
    'total_coins': 'Tổng coin',
  };

  static const _badgeIcons = [
    'emoji_events',
    'star',
    'military_tech',
    'school',
    'auto_awesome',
    'psychology',
    'science',
    'calculate',
    'code',
    'palette',
    'menu_book',
    'trophy',
    'workspace_premium',
    'verified',
    'insights',
  ];

  static const _iconDataMap = {
    'emoji_events': Icons.emoji_events,
    'star': Icons.star,
    'military_tech': Icons.military_tech,
    'school': Icons.school,
    'auto_awesome': Icons.auto_awesome,
    'psychology': Icons.psychology,
    'science': Icons.science,
    'calculate': Icons.calculate,
    'code': Icons.code,
    'palette': Icons.palette,
    'menu_book': Icons.menu_book,
    'trophy': Icons.emoji_events,
    'workspace_premium': Icons.workspace_premium,
    'verified': Icons.verified,
    'insights': Icons.insights,
  };

  IconData _iconData(String name) => _iconDataMap[name] ?? Icons.emoji_events;

  bool _isUrl(String s) => s.contains('/') || s.contains('http');

  Widget _badgeIcon(String iconUrl, double size, {Color? color}) {
    if (iconUrl.isEmpty) return Icon(Icons.emoji_events, size: size, color: color ?? Colors.amber);
    if (_isUrl(iconUrl)) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: Image.network(
          iconUrl,
          width: size,
          height: size,
          errorBuilder: (_, __, ___) => Icon(Icons.emoji_events, size: size, color: color ?? Colors.amber),
          loadingBuilder: (_, child, progress) => progress == null ? child : const SizedBox(width: 24, height: 24, child: CircularProgressIndicator(strokeWidth: 2)),
        ),
      );
    }
    return Icon(_iconData(iconUrl), size: size, color: color ?? Colors.amber);
  }

  @override
  void initState() {
    super.initState();
    _fetchBadges();
  }

  Future<void> _fetchBadges() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/admin/badges');
      final data = response.data;
      final list = _extractList(data);
      _badges = list.map((e) => BadgeDto.fromJson(e as Map<String, dynamic>)).toList();
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

  Future<void> _showBadgeDialog({BadgeDto? badge}) async {
    final isEdit = badge != null;
    final formKey = GlobalKey<FormState>();
    final titleCtrl = TextEditingController(text: badge?.title ?? '');
    final descCtrl = TextEditingController(text: badge?.description ?? '');
    final iconUrlCtrl = TextEditingController(text: badge?.iconUrl ?? '');
    String conditionType = badge?.conditionType ?? _conditionTypes.first;
    final conditionValueCtrl = TextEditingController(text: (badge?.conditionValue ?? 1).toString());

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: Text(isEdit ? 'Sửa huy hiệu' : 'Thêm huy hiệu'),
          content: Form(
            key: formKey,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
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
                    maxLines: 2,
                  ),
                  const SizedBox(height: 12),
                  const Text('Biểu tượng:', style: TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  SizedBox(
                    height: 200,
                    child: SingleChildScrollView(
                      child: Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: _badgeIcons.map((name) {
                          final selected = iconUrlCtrl.text == name;
                          return GestureDetector(
                            onTap: () {
                              setDialogState(() {
                                iconUrlCtrl.text = name;
                              });
                            },
                            child: Container(
                              width: 56,
                              height: 56,
                              decoration: BoxDecoration(
                                color: selected ? Theme.of(ctx).colorScheme.primaryContainer : Colors.grey.withAlpha(20),
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                  color: selected ? Theme.of(ctx).colorScheme.primary : Colors.grey.withAlpha(76),
                                  width: selected ? 2 : 1,
                                ),
                              ),
                              child: Stack(
                                children: [
                                  Center(child: Icon(_iconData(name), size: 28, color: selected ? Theme.of(ctx).colorScheme.primary : Colors.grey)),
                                  if (selected)
                                    Positioned(
                                      top: 2,
                                      right: 2,
                                      child: Icon(Icons.check_circle, size: 16, color: Theme.of(ctx).colorScheme.primary),
                                    ),
                                ],
                              ),
                            ),
                          );
                        }).toList(),
                      ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Center(
                    child: Container(
                      width: 64,
                      height: 64,
                      decoration: BoxDecoration(
                        color: Colors.grey.withAlpha(15),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: _badgeIcon(iconUrlCtrl.text, 36),
                    ),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<String>(
                    value: conditionType,
                    decoration: const InputDecoration(labelText: 'Loại điều kiện *', border: OutlineInputBorder()),
                    items: _conditionTypes
                        .map((t) => DropdownMenuItem(value: t, child: Text(_conditionLabels[t] ?? t)))
                        .toList(),
                    onChanged: (v) {
                      if (v != null) setDialogState(() => conditionType = v);
                    },
                    validator: (v) => (v == null || v.isEmpty) ? 'Vui lòng chọn loại điều kiện' : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: conditionValueCtrl,
                    decoration: InputDecoration(
                      labelText: _getConditionValueLabel(conditionType),
                      border: const OutlineInputBorder(),
                    ),
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) return 'Vui lòng nhập giá trị';
                      if (int.tryParse(v) == null) return 'Vui lòng nhập số';
                      return null;
                    },
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
                    'iconUrl': iconUrlCtrl.text.trim(),
                    'conditionType': conditionType,
                    'conditionValue': conditionValueCtrl.text.trim(),
                  };
                  if (isEdit) {
                    await ApiClient.instance.put('/admin/badges/${badge!.id}', data: body);
                  } else {
                    await ApiClient.instance.post('/admin/badges', data: body);
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
    if (result == true) _fetchBadges();
  }

  String _getConditionValueLabel(String type) {
    switch (type) {
      case 'complete_chapter':
        return 'Số chương cần hoàn thành';
      case 'complete_book':
        return 'Số sách cần hoàn thành';
      case 'perfect_quiz_streak':
        return 'Số lần quiz hoàn hảo liên tiếp';
      case 'total_coins':
        return 'Tổng coin cần đạt';
      default:
        return 'Giá trị điều kiện';
    }
  }

  Future<void> _deleteBadge(BadgeDto badge) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: Text('Bạn có chắc muốn xóa huy hiệu "${badge.title}"?'),
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
      await ApiClient.instance.delete('/admin/badges/${badge.id}');
      _fetchBadges();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa huy hiệu')),
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
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải huy hiệu...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchBadges);
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _fetchBadges,
        child: _badges.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có huy hiệu nào', style: TextStyle(fontSize: 16))),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: _badges.length,
                itemBuilder: (context, index) {
                  final badge = _badges[index];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        leading: SizedBox(width: 48, height: 48, child: _badgeIcon(badge.iconUrl, 48)),
                      title: Text(badge.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (badge.description.isNotEmpty)
                            Text(badge.description, maxLines: 1, overflow: TextOverflow.ellipsis),
                          Text(
                            'Điều kiện: ${_conditionLabels[badge.conditionType] ?? badge.conditionType} = ${badge.conditionValue}',
                            style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
                          ),
                        ],
                      ),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          IconButton(
                            icon: const Icon(Icons.edit),
                            tooltip: 'Sửa',
                            onPressed: () => _showBadgeDialog(badge: badge),
                          ),
                          IconButton(
                            icon: const Icon(Icons.delete, color: Colors.red),
                            tooltip: 'Xóa',
                            onPressed: () => _deleteBadge(badge),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showBadgeDialog(),
        tooltip: 'Thêm huy hiệu',
        child: const Icon(Icons.add),
      ),
    );
  }
}
