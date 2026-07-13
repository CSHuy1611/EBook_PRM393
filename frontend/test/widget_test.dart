import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/math/math_text.dart';
import 'package:flutter/material.dart';

void main() {
  testWidgets('MathText renders plain text', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(home: Scaffold(body: MathText('Hello World'))));
    expect(find.text('Hello World'), findsOneWidget);
  });
}
