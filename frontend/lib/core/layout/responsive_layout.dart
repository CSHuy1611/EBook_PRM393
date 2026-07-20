import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Shared responsive values for phone, tablet and wide layouts.
///
/// Use the available constraints instead of the physical orientation: a split
/// screen tablet in landscape can still be narrow, while a foldable can be
/// wide in portrait.
abstract final class AppBreakpoints {
  static const double compact = 600;
  static const double medium = 900;
  static const double wide = 1200;
}

/// Pure layout calculations, kept separate from widgets so their breakpoint
/// behaviour can be tested without mounting a Flutter view.
abstract final class ResponsiveMetrics {
  static bool isCompactWidth(double width) => width < AppBreakpoints.compact;

  static bool isMediumWidth(double width) =>
      width >= AppBreakpoints.compact && width < AppBreakpoints.medium;

  static bool isWideWidth(double width) => width >= AppBreakpoints.medium;

  static bool isLandscape(Size size) => size.width > size.height;

  static double pageHorizontalPadding(double width) =>
      (width * .05).clamp(16.0, 32.0);

  static double pageVerticalPadding(double width) =>
      isCompactWidth(width) ? 16.0 : 24.0;

  static double contentWidth(double availableWidth, double maxWidth) =>
      math.min(availableWidth, maxWidth);
}

extension ResponsiveBuildContext on BuildContext {
  Size get screenSize => MediaQuery.sizeOf(this);

  bool get isCompact => ResponsiveMetrics.isCompactWidth(screenSize.width);
  bool get isMedium => ResponsiveMetrics.isMediumWidth(screenSize.width);
  bool get isWide => ResponsiveMetrics.isWideWidth(screenSize.width);
  bool get isLandscape => ResponsiveMetrics.isLandscape(screenSize);

  /// Horizontal page spacing that remains comfortable on very narrow phones
  /// and does not grow excessively on tablets or desktop windows.
  EdgeInsets get responsivePagePadding {
    final horizontal = ResponsiveMetrics.pageHorizontalPadding(
      screenSize.width,
    );
    final vertical = ResponsiveMetrics.pageVerticalPadding(screenSize.width);
    return EdgeInsets.symmetric(horizontal: horizontal, vertical: vertical);
  }

  double responsiveGap([double base = 16]) =>
      (base * (isCompact ? .875 : 1)).clamp(8.0, base);
}

/// Centers content and caps its line length on wide or landscape screens.
/// It deliberately only constrains width, so scroll views still receive the
/// full available height and remain usable after rotation.
class ResponsiveContent extends StatelessWidget {
  final Widget child;
  final double maxWidth;
  final Alignment alignment;

  const ResponsiveContent({
    super.key,
    required this.child,
    this.maxWidth = 960,
    this.alignment = Alignment.topCenter,
  });

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final width = ResponsiveMetrics.contentWidth(
          constraints.maxWidth,
          maxWidth,
        );
        return Align(
          alignment: alignment,
          child: SizedBox(width: width, child: child),
        );
      },
    );
  }
}

/// Standard shell for authentication forms.  It keeps fields readable on
/// tablets in landscape and scrollable when the keyboard occupies the screen.
class ResponsiveAuthLayout extends StatelessWidget {
  final Widget child;
  final double maxWidth;

  const ResponsiveAuthLayout({
    super.key,
    required this.child,
    this.maxWidth = 460,
  });

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: LayoutBuilder(
        builder: (context, constraints) {
          final horizontal = (constraints.maxWidth * .06).clamp(16.0, 32.0);
          final vertical = constraints.maxHeight < 520 ? 16.0 : 24.0;
          return Center(
            child: SingleChildScrollView(
              keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
              padding: EdgeInsets.symmetric(
                horizontal: horizontal,
                vertical: vertical,
              ),
              child: ConstrainedBox(
                constraints: BoxConstraints(maxWidth: maxWidth),
                child: child,
              ),
            ),
          );
        },
      ),
    );
  }
}

/// Makes an action bar safe at narrow widths without sacrificing the desktop
/// two-button layout.
class ResponsiveActionBar extends StatelessWidget {
  final Widget? leading;
  final Widget primary;
  final EdgeInsets padding;

  const ResponsiveActionBar({
    super.key,
    this.leading,
    required this.primary,
    this.padding = const EdgeInsets.all(16),
  });

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 360;
        if (compact) {
          return Padding(
            padding: padding,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                ?leading,
                if (leading != null) const SizedBox(height: 8),
                primary,
              ],
            ),
          );
        }
        return Padding(
          padding: padding,
          child: Row(children: [?leading, const Spacer(), primary]),
        );
      },
    );
  }
}
