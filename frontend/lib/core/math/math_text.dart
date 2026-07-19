import 'package:flutter/material.dart';
import 'package:flutter_math_fork/flutter_math.dart';

class MathText extends StatelessWidget {
  final String data;
  final TextStyle? textStyle;
  final TextStyle? mathStyle;

  const MathText(
    this.data, {
    super.key,
    this.textStyle,
    this.mathStyle,
  });

  @override
  Widget build(BuildContext context) {
    try {
      var input = data;
      if (input.contains(r'\') && !input.contains(r'$')) {
        if (input.contains(r'\begin') || input.contains(r'\end')) {
          input = r'$$' + input + r'$$';
        } else {
          input = r'$' + input + r'$';
        }
      }
      final blocks = _parseDisplayMath(input);
      final children = <Widget>[];

      for (final block in blocks) {
        if (block.isMath) {
          children.add(_buildDisplayMath(block.content));
        } else {
          children.addAll(_buildInlineContent(block.content, context));
        }
      }

      if (children.isEmpty) {
        return SelectableText(data, style: textStyle);
      }

      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: children,
      );
    } catch (_) {
      return SelectableText(data, style: textStyle);
    }
  }

  List<_MathBlock> _parseDisplayMath(String input) {
    final result = <_MathBlock>[];
    final regex = RegExp(r'\$\$(.+?)\$\$', dotAll: true);
    int lastEnd = 0;
    for (final match in regex.allMatches(input)) {
      if (match.start > lastEnd) {
        result.add(_MathBlock(text: input.substring(lastEnd, match.start)));
      }
      result.add(_MathBlock(math: match.group(1)!, isDisplay: true));
      lastEnd = match.end;
    }
    if (lastEnd < input.length) {
      result.add(_MathBlock(text: input.substring(lastEnd)));
    }
    return result;
  }

  List<Widget> _buildInlineContent(String text, BuildContext context) {
    final regex = RegExp(r'\$(.+?)\$');
    if (!regex.hasMatch(text)) {
      return [
        SelectableText(
          text,
          style: textStyle,
        ),
      ];
    }

    final spans = <InlineSpan>[];
    int lastEnd = 0;
    for (final match in regex.allMatches(text)) {
      if (match.start > lastEnd) {
        spans.add(TextSpan(text: text.substring(lastEnd, match.start)));
      }
      try {
        spans.add(
          WidgetSpan(
            child: Math.tex(
              match.group(1)!,
              textStyle: mathStyle ??
                  TextStyle(fontSize: 16, fontStyle: FontStyle.italic, color: textStyle?.color ?? DefaultTextStyle.of(context).style.color),
            ),
          ),
        );
      } catch (_) {
        spans.add(TextSpan(text: r'$' + match.group(1)! + r'$'));
      }
      lastEnd = match.end;
    }
    if (lastEnd < text.length) {
      spans.add(TextSpan(text: text.substring(lastEnd)));
    }

    return [
      RichText(
        text: TextSpan(
          style: textStyle ?? DefaultTextStyle.of(context).style,
          children: spans,
        ),
      ),
    ];
  }

  Widget _buildDisplayMath(String latex) {
    try {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Center(
          child: Math.tex(
            latex,
            textStyle:
                mathStyle ?? TextStyle(fontSize: 18, fontWeight: FontWeight.w500, color: textStyle?.color),
          ),
        ),
      );
    } catch (_) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: SelectableText(
          latex,
          style: textStyle?.copyWith(fontFamily: 'monospace'),
        ),
      );
    }
  }
}

class _MathBlock {
  final String? text;
  final String? math;
  final bool isDisplay;

  _MathBlock({this.text, this.math, this.isDisplay = false});

  bool get isMath => math != null;
  String get content => math ?? text ?? '';
}
