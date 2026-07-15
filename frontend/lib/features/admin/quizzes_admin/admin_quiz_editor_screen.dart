import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/network/api_client.dart';

/// Màn hình tạo/sửa quiz bài học (ADM-04)
class AdminQuizEditorScreen extends StatefulWidget {
  final String lessonId;
  final AdminQuizDto? quiz; // null = tạo mới, not null = sửa

  const AdminQuizEditorScreen({
    super.key,
    required this.lessonId,
    this.quiz,
  });

  @override
  State<AdminQuizEditorScreen> createState() => _AdminQuizEditorScreenState();
}

class _AdminQuizEditorScreenState extends State<AdminQuizEditorScreen> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _titleCtrl;
  late TextEditingController _durationCtrl;
  late TextEditingController _passScoreCtrl;
  late TextEditingController _firstPassCoinsCtrl;
  bool _isSaving = false;

  bool get isEdit => widget.quiz != null;

  @override
  void initState() {
    super.initState();
    final quiz = widget.quiz;
    _titleCtrl = TextEditingController(text: quiz?.title ?? '');
    _durationCtrl = TextEditingController(
      text: (quiz?.durationSeconds ?? 600).toString(),
    );
    _passScoreCtrl = TextEditingController(
      text: (quiz?.passScore ?? 5).toString(),
    );
    _firstPassCoinsCtrl = TextEditingController(
      text: (quiz?.firstPassCoins ?? 10).toString(),
    );
  }

  @override
  void dispose() {
    _titleCtrl.dispose();
    _durationCtrl.dispose();
    _passScoreCtrl.dispose();
    _firstPassCoinsCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isSaving = true);
    try {
      final body = {
        'title': _titleCtrl.text.trim(),
        'quizType': 0, // 0 = Lesson, 1 = Chapter
        'lessonId': widget.lessonId,
        'durationSeconds': int.tryParse(_durationCtrl.text) ?? 600,
        'passScore': int.tryParse(_passScoreCtrl.text) ?? 5,
        'firstPassCoins': int.tryParse(_firstPassCoinsCtrl.text) ?? 10,
      };
      if (isEdit) {
        await ApiClient.instance.put('/admin/quizzes/${widget.quiz!.id}', data: body);
      } else {
        await ApiClient.instance.post('/admin/quizzes', data: body);
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(isEdit ? 'Đã cập nhật quiz' : 'Đã tạo quiz')),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}'),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(isEdit ? 'Sửa quiz bài học' : 'Tạo quiz bài học'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: FilledButton.icon(
              onPressed: _isSaving ? null : _save,
              icon: _isSaving
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                    )
                  : const Icon(Icons.save),
              label: Text(isEdit ? 'Cập nhật' : 'Tạo quiz'),
            ),
          ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Header info
            Card(
              color: Theme.of(context).colorScheme.primaryContainer.withAlpha(40),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    const Icon(Icons.info_outline, size: 18),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Quiz bài học: Học sinh làm sau khi học xong lý thuyết. '
                        'Điểm pass và xu thưởng sẽ tự cộng khi học sinh đạt.',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Tên quiz
            TextFormField(
              controller: _titleCtrl,
              decoration: const InputDecoration(
                labelText: 'Tên quiz *',
                hintText: 'VD: Kiểm tra Bài 1 - Hàm số bậc nhất',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.title),
              ),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tên quiz' : null,
            ),
            const SizedBox(height: 16),

            // Thời gian làm bài
            TextFormField(
              controller: _durationCtrl,
              decoration: const InputDecoration(
                labelText: 'Thời gian làm bài (giây) *',
                hintText: 'VD: 600 (= 10 phút)',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.timer),
                suffixText: 'giây',
              ),
              keyboardType: TextInputType.number,
              validator: (v) {
                final val = int.tryParse(v ?? '');
                if (val == null || val < 30) return 'Thời gian tối thiểu 30 giây';
                if (val > 7200) return 'Thời gian tối đa 2 giờ (7200 giây)';
                return null;
              },
              onChanged: (_) => setState(() {}),
            ),
            if (_durationCtrl.text.isNotEmpty) ...[
              const SizedBox(height: 4),
              Padding(
                padding: const EdgeInsets.only(left: 4),
                child: Text(
                  _formatDuration(int.tryParse(_durationCtrl.text) ?? 0),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.blue),
                ),
              ),
            ],
            const SizedBox(height: 16),

            // Ngưỡng pass
            TextFormField(
              controller: _passScoreCtrl,
              decoration: const InputDecoration(
                labelText: 'Ngưỡng điểm pass (thang 10) *',
                hintText: 'VD: 5 (= 5/10)',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.score),
                suffixText: '/10',
              ),
              keyboardType: TextInputType.number,
              validator: (v) {
                final val = int.tryParse(v ?? '');
                if (val == null || val < 1 || val > 10) return 'Ngưỡng pass phải từ 1 đến 10';
                return null;
              },
            ),
            const SizedBox(height: 16),

            // Xu thưởng pass lần đầu
            TextFormField(
              controller: _firstPassCoinsCtrl,
              decoration: const InputDecoration(
                labelText: 'Xu thưởng khi pass lần đầu *',
                hintText: 'VD: 10',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.monetization_on),
                suffixText: 'xu',
              ),
              keyboardType: TextInputType.number,
              validator: (v) {
                final val = int.tryParse(v ?? '');
                if (val == null || val < 0) return 'Xu thưởng không được âm';
                return null;
              },
            ),
            const SizedBox(height: 24),

            // Hướng dẫn thêm câu hỏi
            if (!isEdit) ...[
              Card(
                color: Theme.of(context).colorScheme.tertiaryContainer.withAlpha(40),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.lightbulb_outline, size: 16),
                          const SizedBox(width: 6),
                          Text('Bước tiếp theo', style: Theme.of(context).textTheme.labelMedium),
                        ],
                      ),
                      const SizedBox(height: 6),
                      const Text(
                        'Sau khi tạo quiz, vào màn hình "Câu hỏi" (Questions) để thêm câu hỏi trắc nghiệm. '
                        'Quiz chỉ có thể xuất bản khi có ít nhất 1 câu hỏi.',
                        style: TextStyle(fontSize: 12),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatDuration(int seconds) {
    if (seconds <= 0) return '';
    final min = seconds ~/ 60;
    final sec = seconds % 60;
    if (min == 0) return '= $sec giây';
    if (sec == 0) return '= $min phút';
    return '= $min phút $sec giây';
  }
}
