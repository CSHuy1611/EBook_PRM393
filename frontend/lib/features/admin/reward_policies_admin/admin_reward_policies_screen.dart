import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/reward_policy_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';

class AdminRewardPoliciesScreen extends StatefulWidget {
  const AdminRewardPoliciesScreen({super.key});

  @override
  State<AdminRewardPoliciesScreen> createState() => _AdminRewardPoliciesScreenState();
}

class _AdminRewardPoliciesScreenState extends State<AdminRewardPoliciesScreen> {
  List<RewardPolicyDto> _policies = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchPolicies();
  }

  Future<void> _fetchPolicies() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/admin/reward-policies');
      final data = response.data;
      final list = _extractList(data);
      _policies = list.map((e) => RewardPolicyDto.fromJson(e as Map<String, dynamic>)).toList();
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

  Future<void> _showPolicyDialog({RewardPolicyDto? policy}) async {
    final isEdit = policy != null;
    final formKey = GlobalKey<FormState>();
    final nameCtrl = TextEditingController(text: policy?.name ?? '');
    int quizType = policy?.quizType ?? 1;
    final coinsPerCorrectCtrl = TextEditingController(text: (policy?.coinsPerCorrectAnswer ?? 10).toString());
    final firstPassBonusCtrl = TextEditingController(text: (policy?.firstPassBonusCoins ?? 50).toString());
    final perfectScoreBonusCtrl = TextEditingController(text: (policy?.perfectScoreBonusCoins ?? 20).toString());
    final chapterCompletionBonusCtrl = TextEditingController(text: (policy?.chapterCompletionBonusCoins ?? 100).toString());
    final retryRewardPercentCtrl = TextEditingController(text: (policy?.retryRewardPercent ?? 50).toString());
    final dailyCoinLimitCtrl = TextEditingController(text: policy?.dailyCoinLimit?.toString() ?? '');
    DateTime effectiveFrom = policy?.effectiveFrom ?? DateTime.now();
    bool isActive = policy?.isActive ?? true;

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: Text(isEdit ? 'Sửa chính sách xu' : 'Thêm chính sách xu'),
          content: Form(
            key: formKey,
            child: ConstrainedBox(
              constraints: BoxConstraints(
                maxWidth: MediaQuery.of(ctx).size.width > 600 ? 550 : MediaQuery.of(ctx).size.width - 48,
              ),
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    TextFormField(
                      controller: nameCtrl,
                      decoration: const InputDecoration(labelText: 'Tên chính sách *', border: OutlineInputBorder()),
                      validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tên' : null,
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<int>(
                      value: quizType,
                      decoration: const InputDecoration(labelText: 'Áp dụng cho *', border: OutlineInputBorder()),
                      items: const [
                        DropdownMenuItem(value: 1, child: Text('Bài học (Lesson)')),
                        DropdownMenuItem(value: 2, child: Text('Chương (Chapter)')),
                      ],
                      onChanged: (v) {
                        if (v != null) setDialogState(() => quizType = v);
                      },
                    ),
                    const SizedBox(height: 12),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Thưởng cơ bản', style: TextStyle(fontWeight: FontWeight.bold)),
                            const SizedBox(height: 12),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                TextFormField(
                                  controller: coinsPerCorrectCtrl,
                                  decoration: const InputDecoration(labelText: 'Xu / câu trả lời đúng *', border: OutlineInputBorder()),
                                  keyboardType: TextInputType.number,
                                  validator: (v) => (v == null || int.tryParse(v) == null) ? 'Hợp lệ?' : null,
                                ),
                                const SizedBox(height: 12),
                                TextFormField(
                                  controller: perfectScoreBonusCtrl,
                                  decoration: const InputDecoration(labelText: 'Thưởng đạt điểm tối đa *', border: OutlineInputBorder()),
                                  keyboardType: TextInputType.number,
                                  validator: (v) => (v == null || int.tryParse(v) == null) ? 'Hợp lệ?' : null,
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Thưởng nâng cao & Khác', style: TextStyle(fontWeight: FontWeight.bold)),
                            const SizedBox(height: 12),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                TextFormField(
                                  controller: firstPassBonusCtrl,
                                  decoration: const InputDecoration(labelText: 'Thưởng vượt qua lần đầu *', border: OutlineInputBorder()),
                                  keyboardType: TextInputType.number,
                                  validator: (v) => (v == null || int.tryParse(v) == null) ? 'Hợp lệ?' : null,
                                ),
                                const SizedBox(height: 12),
                                TextFormField(
                                  controller: chapterCompletionBonusCtrl,
                                  decoration: const InputDecoration(labelText: 'Thưởng hoàn thành chương *', border: OutlineInputBorder()),
                                  keyboardType: TextInputType.number,
                                  validator: (v) => (v == null || int.tryParse(v) == null) ? 'Hợp lệ?' : null,
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                TextFormField(
                                  controller: retryRewardPercentCtrl,
                                  decoration: const InputDecoration(labelText: 'Tỷ lệ thưởng làm lại (%) *', border: OutlineInputBorder()),
                                  keyboardType: TextInputType.number,
                                  validator: (v) => (v == null || int.tryParse(v) == null) ? 'Hợp lệ?' : null,
                                ),
                                const SizedBox(height: 12),
                                TextFormField(
                                  controller: dailyCoinLimitCtrl,
                                  decoration: const InputDecoration(labelText: 'Giới hạn xu/ngày (trống = Không)', border: OutlineInputBorder()),
                                  keyboardType: TextInputType.number,
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: InkWell(
                            onTap: () async {
                              final date = await showDatePicker(
                                context: ctx,
                                initialDate: effectiveFrom,
                                firstDate: DateTime(2020),
                                lastDate: DateTime(2030),
                              );
                              if (date != null) {
                                setDialogState(() => effectiveFrom = date);
                              }
                            },
                            child: InputDecorator(
                              decoration: const InputDecoration(labelText: 'Ngày hiệu lực *', border: OutlineInputBorder()),
                              child: Text('${effectiveFrom.day}/${effectiveFrom.month}/${effectiveFrom.year}'),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    // Removed isActive toggle
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
                  final body = {
                    'name': nameCtrl.text.trim(),
                    'quizType': quizType,
                    'coinsPerCorrectAnswer': int.parse(coinsPerCorrectCtrl.text),
                    'firstPassBonusCoins': int.parse(firstPassBonusCtrl.text),
                    'perfectScoreBonusCoins': int.parse(perfectScoreBonusCtrl.text),
                    'chapterCompletionBonusCoins': int.parse(chapterCompletionBonusCtrl.text),
                    'retryRewardPercent': int.parse(retryRewardPercentCtrl.text),
                    'dailyCoinLimit': dailyCoinLimitCtrl.text.trim().isEmpty ? null : int.parse(dailyCoinLimitCtrl.text),
                    'effectiveFrom': effectiveFrom.toUtc().toIso8601String(),
                    'isActive': true,
                  };
                  if (isEdit) {
                    await ApiClient.instance.put('/admin/reward-policies/${policy!.id}', data: body);
                  } else {
                    await ApiClient.instance.post('/admin/reward-policies', data: body);
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
    if (result == true) _fetchPolicies();
  }

  Future<void> _deletePolicy(RewardPolicyDto policy) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: Text('Bạn có chắc muốn xóa chính sách "${policy.name}"?'),
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
      await ApiClient.instance.delete('/admin/reward-policies/${policy.id}');
      _fetchPolicies();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa chính sách')),
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
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải chính sách...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchPolicies);

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _fetchPolicies,
        child: _policies.isEmpty
            ? ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có chính sách nào', style: TextStyle(fontSize: 16))),
                ],
              )
            : ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: _policies.length,
                itemBuilder: (context, index) {
                  final policy = _policies[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: ExpansionTile(
                      title: Row(
                        children: [
                          Icon(
                            policy.isActive ? Icons.check_circle : Icons.cancel,
                            color: policy.isActive ? Colors.green : Colors.red,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              policy.name, 
                              style: const TextStyle(fontWeight: FontWeight.bold),
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 8),
                          Chip(
                            label: Text(policy.quizType == 1 ? 'Bài học' : 'Chương', style: const TextStyle(fontSize: 12)),
                            padding: EdgeInsets.zero,
                          ),
                        ],
                      ),
                      subtitle: Text('Từ: ${policy.effectiveFrom.toString().split('.')[0]} ${policy.effectiveTo != null ? " Đến: ${policy.effectiveTo.toString().split('.')[0]}" : ""}'),
                      children: [
                        Padding(
                          padding: const EdgeInsets.all(16.0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('Xu / câu đúng: ${policy.coinsPerCorrectAnswer}'),
                                  Text('Thưởng đạt 10đ: ${policy.perfectScoreBonusCoins}'),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('Vượt qua lần đầu: ${policy.firstPassBonusCoins}'),
                                  Text('Hoàn thành chương: ${policy.chapterCompletionBonusCoins}'),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('Thưởng khi làm lại: ${policy.retryRewardPercent}%'),
                                  Text('Giới hạn xu/ngày: ${policy.dailyCoinLimit ?? "Không có"}'),
                                ],
                              ),
                              const SizedBox(height: 16),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.end,
                                children: [
                                  OutlinedButton.icon(
                                    icon: const Icon(Icons.edit),
                                    label: const Text('Sửa'),
                                    onPressed: () => _showPolicyDialog(policy: policy),
                                  ),
                                  const SizedBox(width: 8),
                                  FilledButton.icon(
                                    icon: const Icon(Icons.delete),
                                    label: const Text('Xóa'),
                                    style: FilledButton.styleFrom(backgroundColor: Colors.red),
                                    onPressed: () => _deletePolicy(policy),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showPolicyDialog(),
        tooltip: 'Thêm chính sách',
        child: const Icon(Icons.add),
      ),
    );
  }
}
