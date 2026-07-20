import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/layout/responsive_layout.dart';

void main() {
  group('ResponsiveMetrics', () {
    test('classifies compact, medium, and wide widths at breakpoints', () {
      expect(ResponsiveMetrics.isCompactWidth(599), isTrue);
      expect(ResponsiveMetrics.isCompactWidth(600), isFalse);
      expect(ResponsiveMetrics.isMediumWidth(600), isTrue);
      expect(ResponsiveMetrics.isMediumWidth(899), isTrue);
      expect(ResponsiveMetrics.isWideWidth(900), isTrue);
    });

    test('clamps page padding and content width to safe bounds', () {
      expect(ResponsiveMetrics.pageHorizontalPadding(240), 16);
      expect(ResponsiveMetrics.pageHorizontalPadding(500), 25);
      expect(ResponsiveMetrics.pageHorizontalPadding(1200), 32);
      expect(ResponsiveMetrics.pageVerticalPadding(599), 16);
      expect(ResponsiveMetrics.pageVerticalPadding(600), 24);
      expect(ResponsiveMetrics.contentWidth(320, 960), 320);
      expect(ResponsiveMetrics.contentWidth(1400, 960), 960);
    });

    test('uses width and height to identify orientation', () {
      expect(ResponsiveMetrics.isLandscape(const Size(568, 320)), isTrue);
      expect(ResponsiveMetrics.isLandscape(const Size(320, 568)), isFalse);
    });
  });
}
