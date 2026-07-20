import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/layout/responsive_layout.dart';

void main() {
  testWidgets('auth layout remains usable in compact portrait and landscape', (
    tester,
  ) async {
    addTearDown(() => tester.binding.setSurfaceSize(null));

    Future<void> pumpAt(Size size) async {
      await tester.binding.setSurfaceSize(size);
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ResponsiveAuthLayout(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: const [
                  Text('Math IBook'),
                  SizedBox(height: 16),
                  TextField(decoration: InputDecoration(labelText: 'Email')),
                  SizedBox(height: 16),
                  SizedBox(
                    width: double.infinity,
                    child: FilledButton(
                      onPressed: null,
                      child: Text('Đăng nhập'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
      await tester.pump();
      expect(tester.takeException(), isNull);
      expect(find.byType(TextField), findsOneWidget);
    }

    await pumpAt(const Size(320, 568));
    await pumpAt(const Size(568, 320));
  });

  testWidgets('action bar stacks its actions on very narrow screens', (
    tester,
  ) async {
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.binding.setSurfaceSize(const Size(320, 568));

    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ResponsiveActionBar(
            leading: OutlinedButton(onPressed: null, child: Text('Quay lại')),
            primary: FilledButton(onPressed: null, child: Text('Tiếp tục')),
          ),
        ),
      ),
    );

    expect(tester.takeException(), isNull);
    expect(find.byType(Column), findsWidgets);
  });
}
