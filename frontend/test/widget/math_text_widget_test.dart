import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/math/math_text.dart';

Widget createMathText(String text, {TextStyle? textStyle, TextStyle? mathStyle}) {
  return MaterialApp(
    home: Scaffold(
      body: SingleChildScrollView(
        child: MathText(
          text,
          textStyle: textStyle,
          mathStyle: mathStyle,
        ),
      ),
    ),
  );
}

void main() {
  group('MathText widget', () {
    testWidgets('renders plain text correctly', (tester) async {
      await tester.pumpWidget(createMathText('Simple plain text'));

      expect(find.text('Simple plain text'), findsOneWidget);
    });

    testWidgets('renders plain text with custom style', (tester) async {
      const customStyle = TextStyle(fontSize: 20, color: Colors.red, fontWeight: FontWeight.bold);

      await tester.pumpWidget(createMathText('Styled text', textStyle: customStyle));

      final textWidget = tester.widget<SelectableText>(find.byType(SelectableText));
      expect(textWidget.style?.fontSize, equals(20));
    });

    testWidgets('renders inline math expression without crashing', (tester) async {
      await tester.pumpWidget(createMathText(r'Equation: $E=mc^2$'));

      expect(find.textContaining('Equation:'), findsOneWidget);
    });

    testWidgets('renders display math expression without crashing', (tester) async {
      await tester.pumpWidget(createMathText(r'Formula: $$\frac{-b \pm \sqrt{b^2 - 4ac}}{2a}$$'));

      expect(find.textContaining('Formula:'), findsOneWidget);
    });

    testWidgets('handles error gracefully with fallback', (tester) async {
      await tester.pumpWidget(createMathText('Test text'));

      expect(find.text('Test text'), findsOneWidget);
    });

    testWidgets('renders multiple lines with mixed content', (tester) async {
      await tester.pumpWidget(createMathText(
        r'Line with $x$ math and $$\int$$ display',
      ));

      expect(find.textContaining('Line with'), findsOneWidget);
      expect(find.textContaining('math and'), findsOneWidget);
      expect(find.textContaining('display'), findsOneWidget);
    });

    testWidgets('returns SelectableText when children are empty', (tester) async {
      await tester.pumpWidget(createMathText(''));

      expect(find.byType(SelectableText), findsOneWidget);
    });

    testWidgets('uses mathStyle for math expressions', (tester) async {
      const mathStyle = TextStyle(fontSize: 24, fontStyle: FontStyle.italic);

      await tester.pumpWidget(createMathText(
        r'Test $x^2$',
        mathStyle: mathStyle,
      ));

      expect(find.textContaining('Test'), findsOneWidget);
    });
  });
}
