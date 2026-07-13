import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/math/math_text.dart';

void main() {
  group('MathText parser', () {
    testWidgets('renders plain text without math parsing', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText('Hello World')),
      );

      expect(find.text('Hello World'), findsOneWidget);
    });

    testWidgets('parses inline math dollar pattern', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText(r'Text with $x^2$ expression')),
      );

      expect(find.textContaining('Text with'), findsOneWidget);
      expect(find.textContaining('expression'), findsOneWidget);
    });

    testWidgets('parses display math double dollar pattern', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText(r'Display $$\int_a^b f(x)dx$$ here')),
      );

      expect(find.textContaining('Display'), findsOneWidget);
      expect(find.textContaining('here'), findsOneWidget);
    });

    testWidgets('handles multiple inline math expressions', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText(r'$a$ plus $b$ equals $c$')),
      );

      expect(find.textContaining('plus'), findsOneWidget);
      expect(find.textContaining('equals'), findsOneWidget);
    });

    testWidgets('handles mixed inline and display math', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText(r'Inline $x$ and display $$\sum_{i=1}^n i$$')),
      );

      expect(find.textContaining('Inline'), findsOneWidget);
      expect(find.textContaining('and display'), findsOneWidget);
    });

    testWidgets('handles invalid LaTeX gracefully without crash', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText(r'Bad $\invalid{}$ latex')),
      );

      expect(find.textContaining('Bad'), findsOneWidget);
      expect(find.textContaining('latex'), findsOneWidget);
    });

    testWidgets('handles completely empty string', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText('')),
      );
    });

    testWidgets('handles text with dollar signs not as math', (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: MathText(r'Price is $5 and $10')),
      );

      expect(find.textContaining('Price is'), findsOneWidget);
    });
  });
}
