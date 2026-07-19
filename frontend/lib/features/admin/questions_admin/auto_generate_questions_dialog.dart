import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/models/lesson_model.dart';

class AutoGenerateQuestionsDialog extends StatefulWidget {
  const AutoGenerateQuestionsDialog({super.key});

  @override
  State<AutoGenerateQuestionsDialog> createState() => _AutoGenerateQuestionsDialogState();
}

class _AutoGenerateQuestionsDialogState extends State<AutoGenerateQuestionsDialog> {
  List<ChapterModel> _chapters = [];
  List<LessonModel> _lessons = [];
  String? _selectedChapterId;
  String? _selectedLessonId;
  int _count = 5;
  bool _isLoadingChapters = true;
  bool _isLoadingLessons = false;
  bool _isGenerating = false;
  bool _generateForChapter = false; // Add toggle state

  @override
  void initState() {
    super.initState();
    _fetchChapters();
  }

  Future<void> _fetchChapters() async {
    try {
      final response = await ApiClient.instance.get('/admin/chapters');
      if (mounted) {
        setState(() {
          _chapters = (response.data as List).map((x) => ChapterModel.fromJson(x)).toList();
          _isLoadingChapters = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoadingChapters = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi tải chương: $e')));
      }
    }
  }

  Future<void> _fetchLessons(String chapterId) async {
    setState(() {
      _isLoadingLessons = true;
      _selectedLessonId = null;
      _lessons.clear();
    });
    try {
      final response = await ApiClient.instance.get('/admin/lessons/chapter/$chapterId');
      if (mounted) {
        setState(() {
          _lessons = (response.data as List).map((x) => LessonModel.fromJson(x)).toList();
          _isLoadingLessons = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoadingLessons = false);
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi tải bài học: $e')));
      }
    }
  }

  Future<void> _generate() async {
    if (_selectedChapterId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng chọn chương.')),
      );
      return;
    }
    
    if (!_generateForChapter && _selectedLessonId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng chọn bài học.')),
      );
      return;
    }

    setState(() => _isGenerating = true);
    try {
      final data = {
        'chapterId': _selectedChapterId,
        'lessonId': _generateForChapter ? null : _selectedLessonId,
        'count': _count,
      };
      await ApiClient.instance.post('/admin/questions/auto-generate', data: data);
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Đã tạo thành công $_count câu hỏi!')),
        );
        Navigator.pop(context, true); // true indicates success
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: ${e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()}')),
        );
      }
    } finally {
      if (mounted) setState(() => _isGenerating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      elevation: 10,
      backgroundColor: Colors.transparent,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 450),
        child: Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withAlpha(20),
                blurRadius: 15,
                offset: const Offset(0, 5),
              ),
            ],
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
            // Header
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [
                    Theme.of(context).colorScheme.primary,
                    Theme.of(context).colorScheme.tertiary,
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
              ),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: Colors.white.withAlpha(50),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(Icons.auto_awesome, color: Colors.white, size: 28),
                  ),
                  const SizedBox(width: 16),
                  const Expanded(
                    child: Text(
                      'Tạo Câu Hỏi Tự Động',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            
            // Content
            Flexible(
              child: SingleChildScrollView(
                child: Padding(
                  padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.primaryContainer.withAlpha(50),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Theme.of(context).colorScheme.primaryContainer),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.info_outline, color: Theme.of(context).colorScheme.primary, size: 20),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'Hệ thống sẽ gọi API của ChatGPT để tự động tạo câu hỏi trắc nghiệm Toán 8 kèm đáp án và giải thích chi tiết.',
                            style: TextStyle(
                              color: Theme.of(context).colorScheme.onSurfaceVariant,
                              fontSize: 13,
                              height: 1.4,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  
                  // Toggle Cấp độ tạo
                  Text('1. Chọn Cấp Độ', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: RadioListTile<bool>(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Theo Bài Học', style: TextStyle(fontSize: 14)),
                          value: false,
                          groupValue: _generateForChapter,
                          onChanged: (val) => setState(() => _generateForChapter = val!),
                        ),
                      ),
                      Expanded(
                        child: RadioListTile<bool>(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Theo Chương', style: TextStyle(fontSize: 14)),
                          value: true,
                          groupValue: _generateForChapter,
                          onChanged: (val) => setState(() => _generateForChapter = val!),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),

                  // Dropdown Chương
                  Text('2. Chọn Chương', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  _isLoadingChapters 
                    ? const Center(child: Padding(padding: EdgeInsets.all(8), child: CircularProgressIndicator()))
                    : DropdownButtonFormField<String>(
                        decoration: InputDecoration(
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                          filled: true,
                          fillColor: Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(50),
                          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          prefixIcon: const Icon(Icons.menu_book, size: 20),
                        ),
                        hint: const Text('Nhấp để chọn chương...'),
                        value: _selectedChapterId,
                        isExpanded: true,
                        items: _chapters.map((c) => DropdownMenuItem(
                          value: c.id, 
                          child: Text(c.title, overflow: TextOverflow.ellipsis),
                        )).toList(),
                        onChanged: (val) {
                          setState(() => _selectedChapterId = val);
                          if (val != null) _fetchLessons(val);
                        },
                      ),
                  
                  if (!_generateForChapter) ...[
                    const SizedBox(height: 20),
                    
                    // Dropdown Bài Học
                    Text('3. Chọn Bài Học', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 8),
                    _isLoadingLessons
                      ? const Center(child: Padding(padding: EdgeInsets.all(8), child: CircularProgressIndicator()))
                      : DropdownButtonFormField<String>(
                          decoration: InputDecoration(
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                            filled: true,
                            fillColor: _selectedChapterId == null 
                                ? Colors.grey.withAlpha(20) 
                                : Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(50),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                            prefixIcon: const Icon(Icons.play_lesson, size: 20),
                          ),
                          hint: const Text('Nhấp để chọn bài học...'),
                          value: _selectedLessonId,
                          isExpanded: true,
                          items: _lessons.map((l) => DropdownMenuItem(
                            value: l.id, 
                            child: Text(l.title, overflow: TextOverflow.ellipsis),
                          )).toList(),
                          onChanged: _selectedChapterId == null 
                              ? null 
                              : (val) {
                                  setState(() => _selectedLessonId = val);
                                },
                          disabledHint: const Text('Vui lòng chọn chương trước'),
                        ),
                  ],
                  
                  const SizedBox(height: 20),

                  // Dropdown Số lượng
                  Text(_generateForChapter ? '3. Số Lượng Câu Hỏi Cần Tạo' : '4. Số Lượng Câu Hỏi Cần Tạo', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  DropdownButtonFormField<int>(
                    decoration: InputDecoration(
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                      filled: true,
                      fillColor: Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(50),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      prefixIcon: const Icon(Icons.format_list_numbered, size: 20),
                    ),
                    value: _count,
                    isExpanded: true, // Fixed overflow
                    items: [3, 5, 10, 15, 20].map((n) => DropdownMenuItem(
                      value: n, 
                      child: Text('$n câu hỏi (Trắc nghiệm 4 đáp án)'),
                    )).toList(),
                    onChanged: (val) {
                      if (val != null) setState(() => _count = val);
                    },
                  ),
                ],
              ),
            ),
              ),
            ),
            
            // Footer
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(80),
                borderRadius: const BorderRadius.vertical(bottom: Radius.circular(20)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed: () => Navigator.pop(context), 
                    style: TextButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    ),
                    child: const Text('Hủy bỏ'),
                  ),
                  const SizedBox(width: 12),
                  FilledButton.icon(
                    onPressed: _isGenerating ? null : _generate,
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    icon: _isGenerating 
                      ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5)) 
                      : const Icon(Icons.bolt),
                    label: const Text('Bắt đầu tạo', style: TextStyle(fontWeight: FontWeight.bold)),
                  )
                ],
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
}
