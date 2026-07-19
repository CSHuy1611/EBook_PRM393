import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/app_config.dart';
import 'package:math_ibook/core/network/student_feature_api.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';

typedef LeaderboardLoader = Future<LeaderboardModel> Function();

class LeaderboardScreen extends StatefulWidget {
  final LeaderboardLoader? loadLeaderboard;

  const LeaderboardScreen({super.key, this.loadLeaderboard});

  @override
  State<LeaderboardScreen> createState() => _LeaderboardScreenState();
}

class _LeaderboardScreenState extends State<LeaderboardScreen> {
  LeaderboardModel? _data;
  String? _error;
  bool _loading = true;
  bool _refreshing = false;
  bool _requestInFlight = false;
  bool _observedProgress = false;
  int _lastProgressVersion = 0;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final version = context.watch<ProgressNotifier>().version;
    if (!_observedProgress) {
      _observedProgress = true;
      _lastProgressVersion = version;
      return;
    }

    // ProgressNotifier tăng version sau quiz/sync; version mới làm bảng tải lại.
    if (version == _lastProgressVersion) return;
    _lastProgressVersion = version;
    // Đợi frame hiện tại kết thúc để không setState trong build/didChangeDependencies.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _load();
    });
  }

  Future<void> _load() async {
    // Chặn refresh nút, pull-to-refresh và ProgressNotifier chạy đồng thời.
    if (_requestInFlight) return;
    _requestInFlight = true;

    // Nếu đã có data, giữ nội dung cũ và chỉ hiện trạng thái refreshing nhẹ.
    if (mounted && _data != null) {
      setState(() {
        _refreshing = true;
        _error = null;
      });
    }

    try {
      // Test có thể inject loader giả; production dùng StudentFeatureApi.
      final loader =
          widget.loadLeaderboard ?? StudentFeatureApi.instance.getLeaderboard;
      final data = await loader();
      // Chỉ cập nhật state nếu widget vẫn còn trong cây giao diện.
      if (!mounted) return;
      setState(() {
        _data = data;
        _error = null;
        _loading = false;
        _refreshing = false;
      });
    } catch (_) {
      if (!mounted) return;
      const message =
          'Không thể tải bảng xếp hạng. Vui lòng kiểm tra kết nối và thử lại.';
      // Lỗi lần đầu chuyển sang full error; lỗi refresh chỉ hiện SnackBar để giữ data cũ.
      if (_data == null) {
        setState(() {
          _error = message;
          _loading = false;
          _refreshing = false;
        });
      } else {
        setState(() => _refreshing = false);
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text(message)));
      }
    } finally {
      // Luôn mở khóa request kể cả success, error hoặc widget đã unmount.
      _requestInFlight = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    // Loading và error là trạng thái toàn màn hình trước khi có dữ liệu đầu tiên.
    if (_loading) {
      return const AppLoadingWidget(message: 'Đang tải bảng xếp hạng...');
    }
    if (_error != null) {
      return AppErrorWidget(message: _error!, onRetry: _load);
    }

    final data = _data!;
    // Tách ba người đầu cho bục; phần còn lại dùng danh sách lazy.
    final topEntries = data.top100.take(3).toList(growable: false);
    final remainingEntries = data.top100.skip(3).toList(growable: false);

    return LayoutBuilder(
      builder: (context, constraints) {
        return Center(
          child: ConstrainedBox(
            // Giới hạn 760 px để giao diện tablet/web không bị kéo quá rộng.
            constraints: const BoxConstraints(maxWidth: 760),
            child: RefreshIndicator(
              onRefresh: _load,
              // CustomScrollView cho phép các khối header, podium và list cuộn chung.
              child: CustomScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(16, 20, 16, 12),
                    sliver: SliverToBoxAdapter(
                      child: _LeaderboardHeader(
                        updatedAt: data.updatedAt,
                        refreshing: _refreshing,
                        onRefresh: _load,
                      ),
                    ),
                  ),
                  // Thẻ cá nhân nằm ngoài danh sách Top 100 nên vẫn hiện khi rank > 100.
                  if (data.currentUser != null)
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
                      sliver: SliverToBoxAdapter(
                        child: _CurrentUserCard(entry: data.currentUser!),
                      ),
                    ),
                  // Không có Student thì hiển thị empty state thay vì podium rỗng.
                  if (data.top100.isEmpty)
                    const SliverFillRemaining(
                      hasScrollBody: false,
                      child: _EmptyLeaderboard(),
                    )
                  else ...[
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
                      sliver: SliverToBoxAdapter(
                        child: _SectionTitle(
                          title: 'Dẫn đầu bảng xếp hạng',
                          subtitle:
                              '${data.top100.length} học sinh trong Top 100',
                        ),
                      ),
                    ),
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                      sliver: SliverToBoxAdapter(
                        child: _Podium(entries: topEntries),
                      ),
                    ),
                    if (remainingEntries.isNotEmpty) ...[
                      const SliverPadding(
                        padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
                        sliver: SliverToBoxAdapter(
                          child: _SectionTitle(
                            title: 'Top 100 học sinh',
                            subtitle: 'Thành tích toàn trường',
                          ),
                        ),
                      ),
                      SliverPadding(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        // SliverList chỉ dựng row đang cần hiển thị, phù hợp Top 100.
                        sliver: SliverList.separated(
                          itemCount: remainingEntries.length,
                          itemBuilder: (context, index) =>
                              _LeaderboardRow(entry: remainingEntries[index]),
                          separatorBuilder: (_, _) => const Divider(),
                        ),
                      ),
                    ],
                    const SliverToBoxAdapter(child: SizedBox(height: 28)),
                  ],
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}

class _LeaderboardHeader extends StatelessWidget {
  final DateTime updatedAt;
  final bool refreshing;
  final VoidCallback onRefresh;

  const _LeaderboardHeader({
    required this.updatedAt,
    required this.refreshing,
    required this.onRefresh,
  });

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final formattedTime = DateFormat(
      'HH:mm, dd/MM/yyyy',
    ).format(updatedAt.toLocal());

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Bảng xếp hạng',
                style: textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                'Xếp theo tổng xu; nếu bằng xu, ưu tiên nhiều huy hiệu.',
                style: textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                'Dữ liệu lúc $formattedTime',
                style: textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(width: 8),
        IconButton.filledTonal(
          onPressed: refreshing ? null : onRefresh,
          tooltip: 'Làm mới bảng xếp hạng',
          icon: refreshing
              ? const SizedBox.square(
                  dimension: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.refresh_rounded),
        ),
      ],
    );
  }
}

class _CurrentUserCard extends StatelessWidget {
  final LeaderboardEntryModel entry;

  const _CurrentUserCard({required this.entry});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final numberFormat = NumberFormat.decimalPattern('vi');
    final outsideTop100 = entry.rank > 100;

    return Card(
      key: const Key('leaderboard-current-user'),
      color: theme.colorScheme.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              width: 58,
              height: 58,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: theme.colorScheme.primary,
                shape: BoxShape.circle,
              ),
              child: Text(
                '#${entry.rank}',
                style: theme.textTheme.titleMedium?.copyWith(
                  color: theme.colorScheme.onPrimary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Thứ hạng của bạn',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onPrimaryContainer,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    entry.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.titleMedium?.copyWith(
                      color: theme.colorScheme.onPrimaryContainer,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 5),
                  Text(
                    outsideTop100 ? 'Ngoài Top 100' : 'Trong Top 100',
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.onPrimaryContainer,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 12),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                _MetricText(
                  icon: Icons.monetization_on_rounded,
                  value: numberFormat.format(entry.coins),
                  label: 'xu',
                ),
                const SizedBox(height: 6),
                _MetricText(
                  icon: Icons.workspace_premium_rounded,
                  value: '${entry.badgeCount}',
                  label: 'huy hiệu',
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _MetricText extends StatelessWidget {
  final IconData icon;
  final String value;
  final String label;

  const _MetricText({
    required this.icon,
    required this.value,
    required this.label,
  });

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.onPrimaryContainer;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 17, color: color),
        const SizedBox(width: 4),
        Text(
          '$value $label',
          style: Theme.of(context).textTheme.labelLarge?.copyWith(
            color: color,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String title;
  final String subtitle;

  const _SectionTitle({required this.title, required this.subtitle});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          subtitle,
          style: theme.textTheme.bodySmall?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

class _Podium extends StatelessWidget {
  final List<LeaderboardEntryModel> entries;

  const _Podium({required this.entries});

  @override
  Widget build(BuildContext context) {
    final displayedEntries = entries.length == 3
        ? [entries[1], entries[0], entries[2]]
        : entries;

    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 22, 12, 16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            for (final entry in displayedEntries)
              Expanded(child: _PodiumEntry(entry: entry)),
          ],
        ),
      ),
    );
  }
}

class _PodiumEntry extends StatelessWidget {
  final LeaderboardEntryModel entry;

  const _PodiumEntry({required this.entry});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final rankColor = _rankColor(theme.colorScheme, entry.rank);
    final isFirst = entry.rank == 1;

    return Padding(
      padding: EdgeInsets.only(top: isFirst ? 0 : 20),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            isFirst ? Icons.emoji_events_rounded : Icons.military_tech_rounded,
            color: rankColor,
            size: isFirst ? 30 : 25,
          ),
          const SizedBox(height: 6),
          _UserAvatar(entry: entry, radius: isFirst ? 34 : 27),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                '#${entry.rank}',
                style: theme.textTheme.labelLarge?.copyWith(
                  color: rankColor,
                  fontWeight: FontWeight.bold,
                ),
              ),
              if (entry.isCurrentUser) ...[
                const SizedBox(width: 4),
                Icon(
                  Icons.person_pin_rounded,
                  size: 16,
                  color: theme.colorScheme.primary,
                ),
              ],
            ],
          ),
          const SizedBox(height: 3),
          Text(
            entry.name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            '${NumberFormat.decimalPattern('vi').format(entry.coins)} xu',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: theme.textTheme.labelLarge?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          Text(
            '${entry.badgeCount} huy hiệu',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        ],
      ),
    );
  }
}

class _LeaderboardRow extends StatelessWidget {
  final LeaderboardEntryModel entry;

  const _LeaderboardRow({required this.entry});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final numberFormat = NumberFormat.decimalPattern('vi');

    return Container(
      key: Key('leaderboard-row-${entry.userId}'),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
      decoration: entry.isCurrentUser
          ? BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              borderRadius: BorderRadius.circular(12),
            )
          : null,
      child: Row(
        children: [
          SizedBox(
            width: 38,
            child: Text(
              '#${entry.rank}',
              textAlign: TextAlign.center,
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          const SizedBox(width: 8),
          _UserAvatar(entry: entry, radius: 21),
          const SizedBox(width: 11),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        entry.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.bodyLarge?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    if (entry.isCurrentUser) ...[
                      const SizedBox(width: 6),
                      Text(
                        'Bạn',
                        style: theme.textTheme.labelMedium?.copyWith(
                          color: theme.colorScheme.primary,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  '${entry.badgeCount} huy hiệu',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                numberFormat.format(entry.coins),
                style: theme.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              Text(
                'xu',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _UserAvatar extends StatelessWidget {
  final LeaderboardEntryModel entry;
  final double radius;

  const _UserAvatar({required this.entry, required this.radius});

  @override
  Widget build(BuildContext context) {
    final avatarUrl = entry.avatarUrl?.trim();
    final initial = entry.name.trim().isEmpty
        ? '?'
        : entry.name.trim().characters.first.toUpperCase();
    final theme = Theme.of(context);

    return CircleAvatar(
      radius: radius,
      backgroundColor: theme.colorScheme.secondaryContainer,
      foregroundColor: theme.colorScheme.onSecondaryContainer,
      foregroundImage: avatarUrl == null || avatarUrl.isEmpty
          ? null
          : NetworkImage(
              avatarUrl.startsWith('http') ? avatarUrl : '${AppConfig.rootUrl}$avatarUrl'
            ),
      onForegroundImageError: avatarUrl == null || avatarUrl.isEmpty
          ? null
          : (_, _) {},
      child: Text(
        initial,
        style: TextStyle(
          fontWeight: FontWeight.bold,
          color: theme.colorScheme.onSecondaryContainer,
        ),
      ),
    );
  }
}

class _EmptyLeaderboard extends StatelessWidget {
  const _EmptyLeaderboard();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      key: const Key('leaderboard-empty'),
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.leaderboard_outlined,
              size: 64,
              color: theme.colorScheme.primary,
            ),
            const SizedBox(height: 16),
            Text(
              'Chưa có dữ liệu xếp hạng',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              'Hãy hoàn thành bài học và bài kiểm tra để nhận xu.',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

Color _rankColor(ColorScheme colorScheme, int rank) {
  return switch (rank) {
    1 => Colors.amber.shade700,
    2 => colorScheme.secondary,
    3 => Colors.brown.shade500,
    _ => colorScheme.onSurface,
  };
}
