import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/math/math_text.dart';
import 'package:math_ibook/core/math/math_toolbar.dart';
import 'package:math_ibook/core/models/admin_models.dart';
import 'package:math_ibook/core/models/lesson_model.dart';
import 'package:math_ibook/core/network/api_client.dart';

class LessonEditorScreen extends StatefulWidget {
  final String chapterId;
  final LessonModel? lesson;

  const LessonEditorScreen({
    super.key,
    required this.chapterId,
    this.lesson,
  });

  @override
  State<LessonEditorScreen> createState() => _LessonEditorScreenState();
}

class _LessonEditorScreenState extends State<LessonEditorScreen> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _titleCtrl;
  late TextEditingController _contentCtrl;
  late TextEditingController _orderCtrl;
  String _simulationType = '';
  String? _selectedTopicId;
  List<CurriculumTopicDto> _topics = [];
  bool _isSaving = false;
  bool _isLoadingTopics = true;

  final _simulationTypes = ['', 'linear_graph', 'quadratic_graph', 'triangle', 'geogebra', 'desmos', 'phet', 'simulation'];

  bool get isEdit => widget.lesson != null;

  @override
  void initState() {
    super.initState();
    final lesson = widget.lesson;
    _titleCtrl = TextEditingController(text: lesson?.title ?? '');
    _contentCtrl = TextEditingController(text: lesson?.contentBody ?? '');
    _orderCtrl = TextEditingController(text: (lesson?.orderIndex ?? 0).toString());
    _simulationType = lesson?.simulationType ?? '';
    _selectedTopicId = lesson?.curriculumTopicId;
    _loadTopics();
  }

  @override
  void dispose() {
    _titleCtrl.dispose();
    _contentCtrl.dispose();
    _orderCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadTopics() async {
    try {
      final response = await ApiClient.instance.get('/admin/curriculum-topics');
      final data = response.data;
      final list = data is List ? data : (data is Map && data['data'] is List ? data['data'] : []);
      setState(() {
        _topics = (list as List)
            .map((e) => CurriculumTopicDto.fromJson(e as Map<String, dynamic>))
            .where((t) => t.isActive)
            .toList();
        _isLoadingTopics = false;
      });
    } catch (_) {
      if (mounted) setState(() => _isLoadingTopics = false);
    }
  }

  void _insertLatex(String latex) {
    final text = _contentCtrl.text;
    final sel = _contentCtrl.selection;
    final start = sel.isValid ? sel.start : text.length;
    final end = sel.isValid ? sel.end : text.length;
    final insert = _smartLatex(text, start, latex);
    final newText = text.replaceRange(start, end, insert);
    _contentCtrl.value = TextEditingValue(
      text: newText,
      selection: TextSelection.collapsed(offset: start + insert.length),
    );
    setState(() {});
  }

  void _insertTitleLatex(String latex) {
    final text = _titleCtrl.text;
    final sel = _titleCtrl.selection;
    final start = sel.isValid ? sel.start : text.length;
    final end = sel.isValid ? sel.end : text.length;
    final insert = _smartLatex(text, start, latex);
    final newText = text.replaceRange(start, end, insert);
    _titleCtrl.value = TextEditingValue(
      text: newText,
      selection: TextSelection.collapsed(offset: start + insert.length),
    );
    setState(() {});
  }

  bool _inMathMode(String text, int pos) {
    int count = 0;
    for (int i = 0; i < pos; i++) {
      if (text[i] == r'$' && (i == 0 || text[i - 1] != r'\')) count++;
    }
    return count % 2 == 1;
  }

  String _smartLatex(String text, int pos, String latex) {
    if (latex.contains(r'\$')) return latex;
    if (_inMathMode(text, pos)) return latex;
    if (latex.contains(r'\begin') || latex.contains(r'\end')) return r'$$' + latex + r'$$';
    return r'$' + latex + r'$';
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedTopicId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng chọn taxonomy Toán 8')),
      );
      return;
    }
    setState(() => _isSaving = true);
    try {
      final body = {
        'chapterId': widget.chapterId,
        'title': _titleCtrl.text.trim(),
        'contentBody': _contentCtrl.text,
        'simulationType': _simulationType.isEmpty ? null : _simulationType,
        'orderIndex': int.tryParse(_orderCtrl.text) ?? 0,
        'curriculumTopicId': _selectedTopicId,
      };
      if (isEdit) {
        await ApiClient.instance.put('/admin/lessons/${widget.lesson!.id}', data: body);
      } else {
        await ApiClient.instance.post('/admin/lessons', data: body);
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(isEdit ? 'Đã cập nhật bài học' : 'Đã tạo bài học')),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isWide = MediaQuery.of(context).size.width > 800;
    return Scaffold(
      appBar: AppBar(
        title: Text(isEdit ? 'Sửa bài học' : 'Thêm bài học mới'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: FilledButton.icon(
              onPressed: _isSaving ? null : _save,
              icon: _isSaving
                  ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.save),
              label: Text(isEdit ? 'Cập nhật' : 'Tạo'),
            ),
          ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: isWide ? _buildWideLayout() : _buildNarrowLayout(),
      ),
    );
  }

  Widget _buildWideLayout() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(flex: 3, child: _buildEditorPanel()),
          const SizedBox(width: 16),
          Expanded(flex: 2, child: _buildPreviewPanel()),
        ],
      ),
    );
  }

  Widget _buildNarrowLayout() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildEditorFields(),
          const SizedBox(height: 16),
          _buildPreviewPanel(),
        ],
      ),
    );
  }

  Widget _buildEditorPanel() {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildEditorFields(),
        ],
      ),
    );
  }

  Widget _buildEditorFields() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Tiêu đề:', style: TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 4),
        MathToolbar(onInsert: _insertTitleLatex),
        const SizedBox(height: 4),
        TextFormField(
          controller: _titleCtrl,
          decoration: const InputDecoration(labelText: 'Tiêu đề *', border: OutlineInputBorder()),
          validator: (v) => (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tiêu đề' : null,
          onChanged: (_) => setState(() {}),
        ),
        if (_titleCtrl.text.isNotEmpty) ...[
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: Colors.grey.withAlpha(20),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Xem trước tiêu đề:', style: Theme.of(context).textTheme.labelSmall),
                MathText(_titleCtrl.text),
              ],
            ),
          ),
        ],
        const SizedBox(height: 12),
        TextFormField(
          controller: _orderCtrl,
          decoration: const InputDecoration(labelText: 'Thứ tự', border: OutlineInputBorder()),
          keyboardType: TextInputType.number,
        ),
        const SizedBox(height: 12),
        // Taxonomy dropdown
        if (_isLoadingTopics)
          const LinearProgressIndicator()
        else if (_topics.isEmpty)
          const Text(
            'Không tải được taxonomy. Vui lòng thêm Curriculum Topic trước.',
            style: TextStyle(color: Colors.orange, fontSize: 12),
          )
        else
          DropdownButtonFormField<String>(
            value: _selectedTopicId,
            decoration: const InputDecoration(
              labelText: 'Taxonomy Toán 8 *',
              border: OutlineInputBorder(),
            ),
            items: _topics
                .map((t) => DropdownMenuItem(
                      value: t.id,
                      child: Text(t.displayName, overflow: TextOverflow.ellipsis),
                    ))
                .toList(),
            onChanged: (v) => setState(() => _selectedTopicId = v),
            validator: (v) => v == null ? 'Vui lòng chọn taxonomy' : null,
          ),
        const SizedBox(height: 12),
        DropdownButtonFormField<String>(
          value: _simulationType.isEmpty ? null : _simulationType,
          decoration: const InputDecoration(labelText: 'Loại mô phỏng', border: OutlineInputBorder()),
          items: _simulationTypes
              .map((t) => DropdownMenuItem(
                    value: t.isEmpty ? null : t,
                    child: Text(t.isEmpty ? 'Không' : t),
                  ))
              .toList(),
          onChanged: (v) => setState(() => _simulationType = v ?? ''),
        ),
        const SizedBox(height: 16),
        Text('Nội dung (hỗ trợ LaTeX với \$...\$ và \$\$...\$\$)',
            style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: 8),
        MathToolbar(onInsert: _insertLatex),
        const SizedBox(height: 8),
        TextFormField(
          controller: _contentCtrl,
          decoration: const InputDecoration(
            hintText: 'Nhập nội dung bài học...',
            border: OutlineInputBorder(),
            alignLabelWithHint: true,
          ),
          maxLines: 12,
          minLines: 8,
          onChanged: (_) => setState(() {}),
        ),
      ],
    );
  }

  Widget _buildPreviewPanel() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.preview, size: 20),
                const SizedBox(width: 8),
                Text('Xem trước', style: Theme.of(context).textTheme.titleSmall),
              ],
            ),
            const Divider(),
            if (_titleCtrl.text.isNotEmpty) ...[
              Container(
                width: double.maxFinite,
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.primaryContainer.withAlpha(40),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Tiêu đề:', style: Theme.of(context).textTheme.labelSmall),
                    MathText(_titleCtrl.text, textStyle: Theme.of(context).textTheme.titleMedium),
                  ],
                ),
              ),
              const SizedBox(height: 12),
            ],
            if (_contentCtrl.text.isEmpty)
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text('Nhập nội dung để xem trước...', style: TextStyle(color: Colors.grey)),
              )
            else
              MathText(
                _contentCtrl.text,
                textStyle: Theme.of(context).textTheme.bodyLarge,
              ),
          ],
        ),
      ),
    );
  }
}
