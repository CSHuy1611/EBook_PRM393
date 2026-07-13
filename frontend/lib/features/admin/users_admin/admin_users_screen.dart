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
  List<AdminUserDto> _filteredUsers = [];
  bool _isLoading = true;
  String? _error;
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _fetchUsers();
    _searchCtrl.addListener(_filterUsers);
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
      final response = await ApiClient.instance.get('/admin/users');
      final data = response.data;
      final list = _extractList(data);
      _allUsers = list.map((e) => AdminUserDto.fromJson(e as Map<String, dynamic>)).toList();
      _filterUsers();
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

  void _filterUsers() {
    final query = _searchCtrl.text.toLowerCase().trim();
    setState(() {
      if (query.isEmpty) {
        _filteredUsers = List.from(_allUsers);
      } else {
        _filteredUsers = _allUsers.where((u) {
          return u.name.toLowerCase().contains(query) || u.email.toLowerCase().contains(query);
        }).toList();
      }
    });
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
              decoration: InputDecoration(
                hintText: 'Tìm kiếm theo tên hoặc email...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              '${_filteredUsers.length} người dùng',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _fetchUsers,
              child: _filteredUsers.isEmpty
                  ? ListView(
                      children: const [
                        SizedBox(height: 80),
                        Center(child: Text('Không tìm thấy người dùng', style: TextStyle(fontSize: 16))),
                      ],
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(16),
                      itemCount: _filteredUsers.length,
                      itemBuilder: (context, index) {
                        final user = _filteredUsers[index];
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
                                Row(
                                  children: [
                                    _buildInfoChip(Icons.monetization_on, '${user.coins}', Colors.amber),
                                    const SizedBox(width: 8),
                                    _buildInfoChip(Icons.score, '${user.averageScore.toStringAsFixed(1)}%', Colors.green),
                                    const SizedBox(width: 8),
                                    _buildInfoChip(Icons.quiz, '${user.totalQuizAttempts}', Colors.blue),
                                  ],
                                ),
                              ],
                            ),
                            isThreeLine: true,
                            trailing: Chip(
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
                            onTap: () => context.go('/admin/users/${user.id}/history'),
                          ),
                        );
                      },
                    ),
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
