import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/chapter_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/storage/local_db_service.dart';

class ChaptersScreen extends StatefulWidget {
  const ChaptersScreen({super.key});

  @override
  State<ChaptersScreen> createState() => _ChaptersScreenState();
}

class _ChaptersScreenState extends State<ChaptersScreen> {
  List<ChapterModel>? _chapters;
  bool _isLoading = true;
  String? _error;
  int _lastVersion = -1;

  @override
  void initState() {
    super.initState();
    _fetchChapters();
  }

  Future<void> _fetchChapters() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final response = await ApiClient.instance.get('/chapters');
      final List<dynamic> data = response.data as List<dynamic>;
      _chapters = data.map((e) => ChapterModel.fromJson(e as Map<String, dynamic>)).toList();
      await LocalDbService().cacheChapters(_chapters!.map((chapter) => chapter.toJson()).toList());
      setState(() => _isLoading = false);
    } catch (e) {
      final cached = await LocalDbService().getCachedChapters();
      if (cached.isNotEmpty) {
        setState(() { _chapters = cached.map(ChapterModel.fromJson).toList(); _isLoading = false; });
      } else {
        setState(() { _error = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString(); _isLoading = false; });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final version = context.watch<ProgressNotifier>().version;
    if (version != _lastVersion) {
      _lastVersion = version;
      if (_chapters != null && !_isLoading) _fetchChapters();
    }
    if (_isLoading) return const AppLoadingWidget(message: 'Đang tải chương học...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _fetchChapters);
    if (_chapters == null || _chapters!.isEmpty) {
      return const Center(child: Text('Chưa có chương học nào'));
    }
    return RefreshIndicator(
      onRefresh: _fetchChapters,
      child: ListView.builder(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
        itemCount: _chapters!.length,
        itemBuilder: (context, index) {
          final chapter = _chapters![index];
          final showLocked = !chapter.isUnlocked;
          final Color statusColor;
          IconData statusIcon;
          String statusText;

          if (chapter.isQuizPassed) {
            statusColor = const Color(0xFF10B981);
            statusIcon = Icons.check_circle_rounded;
            statusText = 'Đã hoàn thành';
          } else if (chapter.isQuizUnlocked) {
            statusColor = const Color(0xFF3B82F6);
            statusIcon = Icons.lock_open_rounded;
            statusText = 'Sẵn sàng kiểm tra';
          } else if (showLocked) {
            statusColor = const Color(0xFF94A3B8);
            statusIcon = Icons.lock_rounded;
            statusText = 'Bị khóa';
          } else {
            statusColor = const Color(0xFF94A3B8);
            statusIcon = Icons.help_outline_rounded;
            statusText = '';
          }

          return Container(
            margin: const EdgeInsets.only(bottom: 12),
            child: Material(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(14),
              child: InkWell(
                borderRadius: BorderRadius.circular(14),
                onTap: showLocked
                    ? () {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Chương này chưa được mở khóa. Hoàn thành bài kiểm tra chương trước để mở khóa.')),
                        );
                      }
                    : () => context.push('/student/chapters/${chapter.id}'),
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                      color: showLocked ? Theme.of(context).colorScheme.outlineVariant.withAlpha(120) : Theme.of(context).colorScheme.outlineVariant,
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Container(
                            width: 44,
                            height: 44,
                            decoration: BoxDecoration(
                              color: chapter.isQuizPassed
                                  ? const Color(0xFF10B981).withAlpha(20)
                                  : (showLocked ? const Color(0xFFF1F5F9) : const Color(0xFF3B82F6).withAlpha(20)),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Icon(
                              chapter.isQuizPassed ? Icons.check_circle_rounded : Icons.book_rounded,
                              color: chapter.isQuizPassed
                                  ? const Color(0xFF10B981)
                                  : (showLocked ? const Color(0xFF94A3B8) : const Color(0xFF3B82F6)),
                              size: 22,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  chapter.title,
                                  style: TextStyle(
                                      fontWeight: FontWeight.bold,
                                      fontSize: 15,
                                      color: showLocked ? const Color(0xFF94A3B8) : Theme.of(context).colorScheme.onSurface,
                                    ),
                                ),
                                if (chapter.description.isNotEmpty)
                                  Text(
                                      chapter.description,
                                      style: TextStyle(fontSize: 12, color: Theme.of(context).colorScheme.onSurfaceVariant),
                                      maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                              ],
                            ),
                          ),
                          Container(
                            width: 32,
                            height: 32,
                            decoration: BoxDecoration(
                              color: statusColor.withAlpha(20),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: Icon(statusIcon, color: statusColor, size: 18),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      Row(
                        children: [
                          Expanded(
                            child: ClipRRect(
                              borderRadius: BorderRadius.circular(6),
                              child: LinearProgressIndicator(
                                value: chapter.completionPercentage / 100,
                                minHeight: 8,
                                backgroundColor: const Color(0xFFE2E8F0),
                                valueColor: AlwaysStoppedAnimation<Color>(
                                  showLocked ? const Color(0xFFCBD5E1) : const Color(0xFF3B82F6),
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Text(
                            '${chapter.completionPercentage.toStringAsFixed(0)}%',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: showLocked ? const Color(0xFF94A3B8) : Theme.of(context).colorScheme.onSurface,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          Text(
                            'Đã đạt: ${chapter.passedLessonCount}/${chapter.lessonCount} bài',
                            style: TextStyle(fontSize: 12, color: Theme.of(context).colorScheme.onSurfaceVariant, fontWeight: FontWeight.w500),
                          ),
                          if (chapter.relatedBadgeTitle != null && chapter.relatedBadgeTitle!.isNotEmpty) ...[
                            const SizedBox(width: 8),
                            Flexible(
                              child: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFFFF7ED),
                                  borderRadius: BorderRadius.circular(8),
                                  border: Border.all(color: const Color(0xFFFFEDD5)),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    const Icon(Icons.military_tech_rounded, color: Color(0xFFF59E0B), size: 14),
                                    const SizedBox(width: 4),
                                    Flexible(
                                      child: Text(
                                        chapter.relatedBadgeTitle!,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(
                                          fontSize: 11,
                                          fontWeight: FontWeight.w600,
                                          color: Color(0xFFC2410C),
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                          const Spacer(),
                          Text(
                            statusText,
                            style: TextStyle(fontSize: 12, color: statusColor, fontWeight: FontWeight.w600),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
