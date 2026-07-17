import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/progress/progress_notifier.dart';
import 'package:math_ibook/features/student/leaderboard/leaderboard_screen.dart';

LeaderboardEntryModel _entry({
  required int rank,
  required String name,
  required int coins,
  int badgeCount = 0,
  bool isCurrentUser = false,
}) {
  return LeaderboardEntryModel(
    rank: rank,
    userId: 'user-$rank',
    name: name,
    coins: coins,
    badgeCount: badgeCount,
    isCurrentUser: isCurrentUser,
  );
}

LeaderboardModel _leaderboard({
  required List<LeaderboardEntryModel> entries,
  LeaderboardEntryModel? currentUser,
}) {
  return LeaderboardModel(
    top100: entries,
    currentUser: currentUser,
    updatedAt: DateTime.utc(2026, 7, 16, 2, 30),
  );
}

Widget _app({
  required LeaderboardLoader loader,
  ProgressNotifier? progressNotifier,
}) {
  return ChangeNotifierProvider<ProgressNotifier>.value(
    value: progressNotifier ?? ProgressNotifier(),
    child: MaterialApp(
      theme: ThemeData(useMaterial3: true),
      home: Scaffold(body: LeaderboardScreen(loadLeaderboard: loader)),
    ),
  );
}

void main() {
  testWidgets('hiển thị trạng thái đang tải', (tester) async {
    final completer = Completer<LeaderboardModel>();

    await tester.pumpWidget(_app(loader: () => completer.future));

    expect(find.text('Đang tải bảng xếp hạng...'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('hiển thị lỗi thân thiện và cho phép thử lại', (tester) async {
    await tester.pumpWidget(
      _app(loader: () async => throw Exception('network details')),
    );
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Không thể tải bảng xếp hạng. Vui lòng kiểm tra kết nối và thử lại.',
      ),
      findsOneWidget,
    );
    expect(find.text('Thử lại'), findsOneWidget);
    expect(find.textContaining('network details'), findsNothing);
  });

  testWidgets('hiển thị hạng hiện tại, bục Top 3 và danh sách còn lại', (
    tester,
  ) async {
    final entries = [
      _entry(rank: 1, name: 'Hạng Nhất', coins: 5200, badgeCount: 15),
      _entry(rank: 2, name: 'Hạng Nhì', coins: 4800, badgeCount: 13),
      _entry(rank: 3, name: 'Hạng Ba', coins: 4300, badgeCount: 11),
      _entry(rank: 4, name: 'Hạng Tư', coins: 3900, badgeCount: 9),
      _entry(
        rank: 5,
        name: 'Người dùng hiện tại',
        coins: 3500,
        badgeCount: 8,
        isCurrentUser: true,
      ),
    ];

    await tester.pumpWidget(
      _app(
        loader: () async =>
            _leaderboard(entries: entries, currentUser: entries.last),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Bảng xếp hạng'), findsOneWidget);
    expect(find.text('Thứ hạng của bạn'), findsOneWidget);
    expect(find.text('Trong Top 100'), findsOneWidget);
    expect(find.text('Hạng Nhất'), findsOneWidget);
    expect(find.text('Hạng Nhì'), findsOneWidget);
    expect(find.text('Hạng Ba'), findsOneWidget);

    await tester.scrollUntilVisible(
      find.byKey(const Key('leaderboard-row-user-4')),
      200,
    );
    expect(find.byKey(const Key('leaderboard-row-user-4')), findsOneWidget);

    await tester.scrollUntilVisible(
      find.byKey(const Key('leaderboard-row-user-5')),
      100,
    );
    expect(find.byKey(const Key('leaderboard-row-user-5')), findsOneWidget);
  });

  testWidgets('luôn hiển thị người dùng nằm ngoài Top 100', (tester) async {
    final currentUser = _entry(
      rank: 128,
      name: 'Ngoài bảng',
      coins: 120,
      badgeCount: 2,
      isCurrentUser: true,
    );
    final entries = [
      _entry(rank: 1, name: 'Hạng Nhất', coins: 5200),
      _entry(rank: 2, name: 'Hạng Nhì', coins: 4800),
      _entry(rank: 3, name: 'Hạng Ba', coins: 4300),
    ];

    await tester.pumpWidget(
      _app(
        loader: () async =>
            _leaderboard(entries: entries, currentUser: currentUser),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('#128'), findsOneWidget);
    expect(find.text('Ngoài Top 100'), findsOneWidget);
    expect(find.text('Ngoài bảng'), findsOneWidget);
  });

  testWidgets('hiển thị trạng thái trống', (tester) async {
    await tester.pumpWidget(
      _app(loader: () async => _leaderboard(entries: const [])),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('leaderboard-empty')), findsOneWidget);
    expect(find.text('Chưa có dữ liệu xếp hạng'), findsOneWidget);
  });

  testWidgets('làm mới thủ công tải lại dữ liệu', (tester) async {
    var calls = 0;
    final data = _leaderboard(
      entries: [_entry(rank: 1, name: 'Hạng Nhất', coins: 5200)],
    );

    await tester.pumpWidget(
      _app(
        loader: () async {
          calls++;
          return data;
        },
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Làm mới bảng xếp hạng'));
    await tester.pumpAndSettle();

    expect(calls, 2);
  });

  testWidgets('tự tải lại khi tiến trình học tập thay đổi', (tester) async {
    var calls = 0;
    final notifier = ProgressNotifier();
    final data = _leaderboard(
      entries: [_entry(rank: 1, name: 'Hạng Nhất', coins: 5200)],
    );

    await tester.pumpWidget(
      _app(
        progressNotifier: notifier,
        loader: () async {
          calls++;
          return data;
        },
      ),
    );
    await tester.pumpAndSettle();

    notifier.notifyProgressChanged();
    await tester.pumpAndSettle();

    expect(calls, 2);
  });
}
