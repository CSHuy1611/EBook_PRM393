import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:math_ibook/core/math/math_text.dart';
import 'package:math_ibook/core/models/quiz_models.dart';

class QuizResultScreen extends StatefulWidget {
  final String attemptId;

  const QuizResultScreen({super.key, required this.attemptId});

  @override
  State<QuizResultScreen> createState() => _QuizResultScreenState();
}

class _QuizResultScreenState extends State<QuizResultScreen> with SingleTickerProviderStateMixin {
  QuizResultDto? _result;
  late AnimationController _coinController;
  late Animation<double> _coinAnimation;
  int _displayCoins = 0;

  @override
  void initState() {
    super.initState();
    _coinController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    );
    _coinAnimation = CurvedAnimation(parent: _coinController, curve: Curves.easeOutCubic);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_result == null) {
      final extra = GoRouterState.of(context).extra;
      if (extra is QuizResultDto) {
        _result = extra;
        _coinController.addListener(() {
          final target = _result!.coinsEarned.toDouble();
          setState(() {
            _displayCoins = (_coinAnimation.value * target).round();
          });
        });
        _coinController.forward();
        _checkNewBadges();
      }
    }
  }

  @override
  void dispose() {
    _coinController.dispose();
    super.dispose();
  }

  void _checkNewBadges() {
    if (_result == null || _result!.newBadges.isEmpty) return;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _showBadgeDialog();
    });
  }

  void _showBadgeDialog() {
    if (_result == null || !mounted) return;
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.emoji_events, color: Colors.amber, size: 28),
            SizedBox(width: 8),
            Text('Danh hiệu mới!'),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: _result!.newBadges.map((badge) => Padding(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Column(
              children: [
                Icon(Icons.military_tech, size: 48, color: Colors.amber.shade700),
                const SizedBox(height: 8),
                Text(badge.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                if (badge.description.isNotEmpty) Text(badge.description, textAlign: TextAlign.center),
              ],
            ),
          )).toList(),
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Tuyệt vời!'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_result == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Kết quả')),
        body: const Center(child: Text('Không có dữ liệu kết quả')),
      );
    }

    final result = _result!;
    final percentage = result.totalQuestions > 0 ? result.score / result.totalQuestions : 0.0;
    final passed = percentage >= 0.5;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Kết quả bài kiểm tra'),
        automaticallyImplyLeading: false,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            const SizedBox(height: 16),
            SizedBox(
              width: 180,
              height: 180,
              child: CustomPaint(
                painter: _CircularScorePainter(
                  percentage: percentage,
                  passed: passed,
                  score: result.score,
                  total: result.totalQuestions,
                ),
              ),
            ),
            const SizedBox(height: 24),
            if (result.coinsEarned > 0) ...[
              AnimatedBuilder(
                animation: _coinAnimation,
                builder: (context, child) {
                  return Container(
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    decoration: BoxDecoration(
                      color: Colors.amber.withAlpha(30),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: Colors.amber.shade300),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.monetization_on, color: Colors.amber, size: 28),
                        const SizedBox(width: 8),
                        Text(
                          '+$_displayCoins',
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                            color: Colors.amber.shade800,
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
              const SizedBox(height: 24),
            ],
            Divider(),
            const SizedBox(height: 16),
            Text(
              'Chi tiết câu trả lời',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            ...result.correctAnswers.asMap().entries.map((entry) {
              final idx = entry.key;
              final answer = entry.value;
              final isCorrect = answer.isCorrect;
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(
                            isCorrect ? Icons.check_circle : Icons.cancel,
                            color: isCorrect ? Colors.green : Colors.red,
                            size: 20,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'Câu ${idx + 1}',
                              style: const TextStyle(fontWeight: FontWeight.w600),
                            ),
                          ),
                          Text(
                            isCorrect ? 'Đúng' : 'Sai',
                            style: TextStyle(
                              color: isCorrect ? Colors.green : Colors.red,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                      if (!isCorrect) ...[
                        const SizedBox(height: 8),
                        Text(
                          'Đáp án đúng: ${String.fromCharCode(65 + answer.correctOption)}',
                          style: TextStyle(color: Colors.green.shade700),
                        ),
                        Text(
                          'Bạn chọn: ${String.fromCharCode(65 + answer.selectedOption)}',
                          style: TextStyle(color: Colors.red.shade700),
                        ),
                      ],
                      if (answer.explanation.isNotEmpty) ...[
                        const SizedBox(height: 8),
                        MathText(
                          answer.explanation,
                          textStyle: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Theme.of(context).colorScheme.onSurfaceVariant,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              );
            }),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: () => context.pop(),
                icon: const Icon(Icons.arrow_back),
                label: const Text('Tiếp tục'),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                ),
              ),
            ),
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }
}

class _CircularScorePainter extends CustomPainter {
  final double percentage;
  final bool passed;
  final double score;
  final int total;

  _CircularScorePainter({
    required this.percentage,
    required this.passed,
    required this.score,
    required this.total,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final cx = size.width / 2;
    final cy = size.height / 2;
    final radius = size.width / 2 - 8;

    final bgPaint = Paint()
      ..color = Colors.grey.withAlpha(40)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 16;

    canvas.drawCircle(Offset(cx, cy), radius, bgPaint);

    final scorePaint = Paint()
      ..color = passed ? Colors.green : Colors.red
      ..style = PaintingStyle.stroke
      ..strokeWidth = 16
      ..strokeCap = StrokeCap.round;

    final sweepAngle = (percentage * 360).clamp(0.0, 360.0) * (math.pi / 180);
    canvas.drawArc(
      Rect.fromCircle(center: Offset(cx, cy), radius: radius),
      -math.pi / 2,
      sweepAngle,
      false,
      scorePaint,
    );

    final textPainter = TextPainter(textDirection: TextDirection.ltr);
    textPainter.text = TextSpan(
      text: score % 1 == 0 ? '${score.toInt()}/$total' : '$score/$total',
      style: const TextStyle(fontSize: 28, fontWeight: FontWeight.bold),
    );
    textPainter.layout();
    textPainter.paint(canvas, Offset(cx - textPainter.width / 2, cy - textPainter.height / 2));
  }

  @override
  bool shouldRepaint(covariant _CircularScorePainter old) =>
      old.percentage != percentage || old.score != score || old.total != total;
}
