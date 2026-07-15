import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'admin_quiz_editor_screen.dart';

/// Màn hình xem/quản lý quiz của một bài học cụ thể (ADM-04)
class AdminLessonQuizScreen extends StatefulWidget {
  final String lessonId;
  final String lessonTitle;

  const AdminLessonQuizScreen({
    super.key,
    required this.lessonId,
    required this.lessonTitle,
  });

  @override
  State<AdminLessonQuizScreen> createState() => _AdminLessonQuizScreenState();
}

class _AdminLessonQuizScreenState extends State<AdminLessonQuizScreen> {
  AdminQuizDto? _quiz;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchQuiz();
  }

  Future<void> _fetchQuiz() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance
          .get('/admin/quizzes', queryParameters: {'lessonId': widget.lessonId});
      final data = response.data;
      final list = data is List ? data : (data is Map && data['data'] is List ? data['data'] : []);
      if ((list as List).isNotEmpty) {
        _quiz = AdminQuizDto.fromJson(list.first as Map<String, dynamic>);
      } else {
        _quiz = null;
      }
    } catch (e) {
      _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _togglePublish() async {
    if (_quiz == null) return;
    try {
      await ApiClient.instance.patch('/admin/quizzes/${_quiz!.id}/publish', data: {});
      _fetchQuiz();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(_quiz!.isPublished ? 'Đã ẩn quiz' : 'Đã xuất bản quiz'),
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

  Future<void> _deleteQuiz() async {
    if (_quiz == null) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xác nhận xóa quiz'),
        content: Text('Bạn có chắc muốn xóa quiz "${_quiz!.title}"?'),
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
      await ApiClient.instance.delete('/admin/quizzes/${_quiz!.id}');
      _fetchQuiz();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa quiz')),
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

  Future<void> _openEditor() async {
    final result = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => AdminQuizEditorScreen(
          lessonId: widget.lessonId,
          quiz: _quiz,
        ),
      ),
    );
    if (result == true) _fetchQuiz();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Quản lý Quiz bài học', style: TextStyle(fontSize: 16)),
            Text(
              widget.lessonTitle,
              style: Theme.of(context).textTheme.bodySmall,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
      body: _isLoading
          ? const AppLoadingWidget(message: 'Đang tải quiz...')
          : _error != null
              ? AppErrorWidget(message: _error!, onRetry: _fetchQuiz)
              : RefreshIndicator(
                  onRefresh: _fetchQuiz,
                  child: _quiz == null ? _buildNoQuizView() : _buildQuizView(),
                ),
      floatingActionButton: _quiz == null && !_isLoading && _error == null
          ? FloatingActionButton.extended(
              onPressed: _openEditor,
              icon: const Icon(Icons.add),
              label: const Text('Tạo quiz'),
            )
          : null,
    );
  }

  Widget _buildNoQuizView() {
    return ListView(
      children: [
        const SizedBox(height: 80),
        Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.quiz_outlined, size: 64, color: Colors.grey.shade400),
              const SizedBox(height: 16),
              Text(
                'Bài học này chưa có quiz',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(color: Colors.grey.shade600),
              ),
              const SizedBox(height: 8),
              Text(
                'Nhấn nút "Tạo quiz" để thêm quiz cho bài học này.',
                style: Theme.of(context).textTheme.bodySmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: _openEditor,
                icon: const Icon(Icons.add),
                label: const Text('Tạo quiz ngay'),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildQuizView() {
    final quiz = _quiz!;
    final durationMin = quiz.durationSeconds ~/ 60;
    final durationSec = quiz.durationSeconds % 60;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Status banner
        _buildStatusBanner(quiz),
        const SizedBox(height: 16),

        // Quiz info card
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(Icons.quiz, size: 20),
                    const SizedBox(width: 8),
                    Text('Thông tin Quiz', style: Theme.of(context).textTheme.titleMedium),
                  ],
                ),
                const Divider(),
                _buildInfoRow(Icons.title, 'Tên quiz', quiz.title),
                _buildInfoRow(Icons.timer, 'Thời gian', '$durationMin phút${durationSec > 0 ? ' $durationSec giây' : ''}'),
                _buildInfoRow(Icons.score, 'Ngưỡng pass', '${quiz.passScore}/10'),
                _buildInfoRow(Icons.monetization_on, 'Xu thưởng pass lần đầu', '${quiz.firstPassCoins} xu'),
                _buildInfoRow(Icons.help_outline, 'Số câu hỏi', '${quiz.questionCount} câu'),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),

        // Action buttons
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: _openEditor,
                icon: const Icon(Icons.edit),
                label: const Text('Sửa quiz'),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: FilledButton.icon(
                onPressed: _togglePublish,
                icon: Icon(quiz.isPublished ? Icons.visibility_off : Icons.visibility),
                label: Text(quiz.isPublished ? 'Ẩn quiz' : 'Xuất bản'),
                style: quiz.isPublished
                    ? FilledButton.styleFrom(backgroundColor: Colors.orange)
                    : null,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: _deleteQuiz,
          icon: const Icon(Icons.delete, color: Colors.red),
          label: const Text('Xóa quiz', style: TextStyle(color: Colors.red)),
          style: OutlinedButton.styleFrom(side: const BorderSide(color: Colors.red)),
        ),

        // Tips
        const SizedBox(height: 24),
        Card(
          color: Theme.of(context).colorScheme.surfaceContainerHighest,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(Icons.info_outline, size: 16),
                    const SizedBox(width: 6),
                    Text('Lưu ý', style: Theme.of(context).textTheme.labelMedium),
                  ],
                ),
                const SizedBox(height: 6),
                const Text(
                  '• Quiz chỉ xuất bản được khi có ít nhất 1 câu hỏi hợp lệ.\n'
                  '• Khi sửa câu hỏi, quiz sẽ tự động chuyển về trạng thái Draft.\n'
                  '• Quản lý câu hỏi chi tiết qua màn hình "Questions".',
                  style: TextStyle(fontSize: 12),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildStatusBanner(AdminQuizDto quiz) {
    final isPublished = quiz.isPublished;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: BoxDecoration(
        color: isPublished ? Colors.green.withAlpha(25) : Colors.orange.withAlpha(25),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: isPublished ? Colors.green : Colors.orange,
          width: 0.5,
        ),
      ),
      child: Row(
        children: [
          Icon(
            isPublished ? Icons.check_circle : Icons.pending,
            color: isPublished ? Colors.green : Colors.orange,
            size: 18,
          ),
          const SizedBox(width: 8),
          Text(
            isPublished ? 'Quiz đã xuất bản — học sinh có thể làm bài' : 'Quiz đang ở trạng thái Draft — chưa hiển thị với học sinh',
            style: TextStyle(
              color: isPublished ? Colors.green.shade700 : Colors.orange.shade700,
              fontSize: 13,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Icon(icon, size: 16, color: Theme.of(context).colorScheme.primary),
          const SizedBox(width: 8),
          SizedBox(width: 150, child: Text(label, style: const TextStyle(color: Colors.grey))),
          Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}
