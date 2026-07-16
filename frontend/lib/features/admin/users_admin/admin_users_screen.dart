import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminUsersScreen extends StatefulWidget {
  const AdminUsersScreen({super.key});

  @override
  State<AdminUsersScreen> createState() => _AdminUsersScreenState();
}

class _AdminUsersScreenState extends State<AdminUsersScreen> {
  List<AdminUserDto> _allUsers = [];
  bool _isLoading = true;
  String? _error;
  final _searchCtrl = TextEditingController();
  
  int _currentPage = 1;
  int _totalItems = 0;
  final int _pageSize = 20;

  @override
  void initState() {
    super.initState();
    _fetchUsers();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _fetchUsers() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final queryParams = <String, dynamic>{
        'page': _currentPage,
        'limit': _pageSize,
      };
      if (_searchCtrl.text.trim().isNotEmpty) {
        queryParams['search'] = _searchCtrl.text.trim();
      }
      
      final response = await ApiClient.instance.get('/admin/users', queryParameters: queryParams);
      final data = response.data;
      if (data is Map<String, dynamic> && data.containsKey('items')) {
         _totalItems = data['total'] ?? 0;
         final list = data['items'] as List;
         _allUsers = list.map((e) => AdminUserDto.fromJson(e as Map<String, dynamic>)).toList();
      } else {
         final list = _extractList(data);
         _allUsers = list.map((e) => AdminUserDto.fromJson(e as Map<String, dynamic>)).toList();
         _totalItems = _allUsers.length;
      }
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

  void _performSearch() {
    setState(() {
      _currentPage = 1;
    });
    _fetchUsers();
  }

  Future<void> _showStatusDialog(AdminUserDto user) async {
    final isLocking = user.isActive;
    final reasonCtrl = TextEditingController();
    final formKey = GlobalKey<FormState>();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isLocking ? 'Khóa tài khoản' : 'Mở khóa tài khoản'),
        content: Form(
          key: formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(isLocking 
                  ? 'Bạn có chắc chắn muốn khóa tài khoản của ${user.name} không?'
                  : 'Bạn có chắc chắn muốn mở khóa tài khoản của ${user.name} không?'),
              const SizedBox(height: 16),
              TextFormField(
                controller: reasonCtrl,
                decoration: const InputDecoration(
                  labelText: 'Lý do *',
                  border: OutlineInputBorder(),
                ),
                validator: (v) => v == null || v.trim().isEmpty ? 'Vui lòng nhập lý do' : null,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          ElevatedButton(
            onPressed: () {
              if (formKey.currentState!.validate()) {
                Navigator.pop(ctx, true);
              }
            },
            style: ElevatedButton.styleFrom(backgroundColor: isLocking ? Colors.red : Colors.green),
            child: Text(isLocking ? 'Khóa' : 'Mở khóa', style: const TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      try {
        setState(() => _isLoading = true);
        await ApiClient.instance.patch(
          '/admin/users/${user.id}/status',
          data: {'isActive': !isLocking, 'reason': reasonCtrl.text.trim()},
        );
        await _fetchUsers();
      } catch (e) {
        setState(() => _isLoading = false);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
          );
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải người dùng...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchUsers);
    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            child: TextField(
              controller: _searchCtrl,
              onSubmitted: (_) => _performSearch(),
              decoration: InputDecoration(
                hintText: 'Tìm kiếm theo tên hoặc email...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: () {
                    _searchCtrl.clear();
                    _performSearch();
                  },
                ),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              'Tổng cộng: $_totalItems học sinh',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _fetchUsers,
              child: _allUsers.isEmpty
                  ? ListView(
                      children: const [
                        SizedBox(height: 80),
                        Center(child: Text('Không tìm thấy người dùng', style: TextStyle(fontSize: 16))),
                      ],
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(16),
                      itemCount: _allUsers.length,
                      itemBuilder: (context, index) {
                        final user = _allUsers[index];
                        return Card(
                          margin: const EdgeInsets.only(bottom: 8),
                          child: ListTile(
                            leading: CircleAvatar(
                              backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                              child: Text(
                                user.name.isNotEmpty ? user.name[0].toUpperCase() : '?',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                            ),
                            title: Text(user.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(user.email),
                                const SizedBox(height: 2),
                                Wrap(
                                  spacing: 8,
                                  runSpacing: 4,
                                  children: [
                                    _buildInfoChip(Icons.monetization_on, '${user.coins}', Colors.amber),
                                    _buildInfoChip(Icons.score, '${user.averageScore.toStringAsFixed(1)}%', Colors.green),
                                    _buildInfoChip(Icons.quiz, '${user.totalQuizAttempts}', Colors.blue),
                                    _buildInfoChip(Icons.emoji_events, '${user.badgeCount}', Colors.purple),
                                    if (user.rank != null)
                                      _buildInfoChip(Icons.leaderboard, '#${user.rank}', Colors.deepOrange),
                                    if (!user.isActive)
                                      _buildInfoChip(Icons.lock, 'Bị khóa', Colors.red),
                                  ],
                                ),
                              ],
                            ),
                            isThreeLine: true,
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Chip(
                                  label: Text(
                                    user.role,
                                    style: TextStyle(
                                      fontSize: 11,
                                      color: user.role == 'Admin' ? Colors.orange : Colors.blue,
                                    ),
                                  ),
                                  backgroundColor: (user.role == 'Admin' ? Colors.orange : Colors.blue).withAlpha(25),
                                  side: BorderSide.none,
                                  visualDensity: VisualDensity.compact,
                                ),
                                IconButton(
                                  icon: Icon(
                                    user.isActive ? Icons.lock_open : Icons.lock,
                                    color: user.isActive ? Colors.green : Colors.red,
                                  ),
                                  tooltip: user.isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản',
                                  onPressed: () => _showStatusDialog(user),
                                ),
                              ],
                            ),
                            onTap: () => context.go('/admin/users/${user.id}/history'),
                          ),
                        );
                      },
                    ),
            ),
          ),
          if (_totalItems > _pageSize)
            Padding(
              padding: const EdgeInsets.all(8.0),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  IconButton(
                    icon: const Icon(Icons.chevron_left),
                    onPressed: _currentPage > 1
                        ? () {
                            setState(() => _currentPage--);
                            _fetchUsers();
                          }
                        : null,
                  ),
                  Text('Trang $_currentPage / ${(_totalItems / _pageSize).ceil()}'),
                  IconButton(
                    icon: const Icon(Icons.chevron_right),
                    onPressed: _currentPage < (_totalItems / _pageSize).ceil()
                        ? () {
                            setState(() => _currentPage++);
                            _fetchUsers();
                          }
                        : null,
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildInfoChip(IconData icon, String label, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withAlpha(20),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: color),
          const SizedBox(width: 2),
          Text(label, style: TextStyle(fontSize: 11, color: color, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}
