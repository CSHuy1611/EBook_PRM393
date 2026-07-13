import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:math_ibook/core/math/math_text.dart';

class SimulationWidget extends StatelessWidget {
  final String simulationType;

  const SimulationWidget({super.key, required this.simulationType});

  @override
  Widget build(BuildContext context) {
    switch (simulationType) {
      case 'linear_graph':
        return const LinearGraphSimulator();
      case 'quadratic_graph':
        return const QuadraticGraphSimulator();
      case 'triangle':
        return const TriangleSimulator();
      default:
        return const SizedBox.shrink();
    }
  }
}

class LinearGraphSimulator extends StatefulWidget {
  const LinearGraphSimulator({super.key});

  @override
  State<LinearGraphSimulator> createState() => _LinearGraphSimulatorState();
}

class _LinearGraphSimulatorState extends State<LinearGraphSimulator> {
  double _a = 1.0;
  double _b = 0.0;

  @override
  Widget build(BuildContext context) {
    final equation = r'y = ' '${_a.toStringAsFixed(1)}x ' '${_b >= 0 ? '+ ' : '- '}${_b.abs().toStringAsFixed(1)}';
    return Column(
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                SizedBox(
                  height: 250,
                  child: CustomPaint(
                    size: const Size(double.infinity, 250),
                    painter: _LinearGraphPainter(a: _a, b: _b),
                  ),
                ),
                const SizedBox(height: 16),
                MathText(equation, textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
              ],
            ),
          ),
        ),
        _SliderRow(label: 'a (hệ số góc)', value: _a, min: -5, max: 5, onChanged: (v) => setState(() => _a = v)),
        _SliderRow(label: 'b (hệ số tự do)', value: _b, min: -10, max: 10, onChanged: (v) => setState(() => _b = v)),
      ],
    );
  }
}

class _SliderRow extends StatelessWidget {
  final String label;
  final double value;
  final double min;
  final double max;
  final ValueChanged<double> onChanged;

  const _SliderRow({
    required this.label,
    required this.value,
    required this.min,
    required this.max,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(width: 140, child: Text(label, style: const TextStyle(fontSize: 14))),
          Expanded(
            child: Slider(value: value, min: min, max: max, divisions: ((max - min) * 10).toInt(), onChanged: onChanged),
          ),
          SizedBox(
            width: 50,
            child: Text(value.toStringAsFixed(1), textAlign: TextAlign.right, style: const TextStyle(fontWeight: FontWeight.w600)),
          ),
        ],
      ),
    );
  }
}

class _LinearGraphPainter extends CustomPainter {
  final double a;
  final double b;

  _LinearGraphPainter({required this.a, required this.b});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..color = Colors.black87..strokeWidth = 1;
    final gridPaint = Paint()..color = Colors.grey.withAlpha(50)..strokeWidth = 0.5;
    final graphPaint = Paint()..color = Colors.blue..strokeWidth = 2.5..style = PaintingStyle.stroke;

    final cx = size.width / 2;
    final cy = size.height / 2;
    const scale = 25.0;

    canvas.translate(cx, cy);

    canvas.drawLine(Offset(-size.width / 2, 0), Offset(size.width / 2, 0), paint);
    canvas.drawLine(Offset(0, -size.height / 2), Offset(0, size.height / 2), paint);

    for (double i = -(size.width / 2 / scale); i <= (size.width / 2 / scale); i += 1) {
      final x = i * scale;
      canvas.drawLine(Offset(x, -size.height / 2), Offset(x, size.height / 2), gridPaint);
    }
    for (double i = -(size.height / 2 / scale); i <= (size.height / 2 / scale); i += 1) {
      final y = i * scale;
      canvas.drawLine(Offset(-size.width / 2, y), Offset(size.width / 2, y), gridPaint);
    }

    final path = Path();
    double startX = -size.width / 2;
    double endX = size.width / 2;

    double yStart = -(a * (startX / scale) + b) * scale;
    if (yStart.isFinite) {
      path.moveTo(startX, yStart.clamp(-size.height / 2, size.height / 2));
    }

    for (double px = startX; px <= endX; px += 2) {
      final val = a * (px / scale) + b;
      double py = -val * scale;
      if (py.isFinite) {
        if (py > size.height / 2 || py < -size.height / 2) {
          py = py.clamp(-size.height / 2, size.height / 2);
        }
        path.lineTo(px, py);
      }
    }

    canvas.drawPath(path, graphPaint);
  }

  @override
  bool shouldRepaint(covariant _LinearGraphPainter old) => old.a != a || old.b != b;
}

class QuadraticGraphSimulator extends StatefulWidget {
  const QuadraticGraphSimulator({super.key});

  @override
  State<QuadraticGraphSimulator> createState() => _QuadraticGraphSimulatorState();
}

class _QuadraticGraphSimulatorState extends State<QuadraticGraphSimulator> {
  double _a = 1.0;
  double _b = 0.0;
  double _c = 0.0;

  @override
  Widget build(BuildContext context) {
    final vertexX = -_b / (2 * _a);
    final vertexY = _a * vertexX * vertexX + _b * vertexX + _c;
    final equation = r'y = ' '${_a.toStringAsFixed(1)}x^2 ' '${_b >= 0 ? '+ ' : '- '}${_b.abs().toStringAsFixed(1)}x '
        '${_c >= 0 ? '+ ' : '- '}${_c.abs().toStringAsFixed(1)}';
    final vertexLatex = r'\text{Đỉnh: } (' '${vertexX.toStringAsFixed(2)}, ${vertexY.toStringAsFixed(2)}' ')';

    return Column(
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                SizedBox(
                  height: 250,
                  child: CustomPaint(
                    size: const Size(double.infinity, 250),
                    painter: _QuadraticGraphPainter(a: _a, b: _b, c: _c),
                  ),
                ),
                const SizedBox(height: 16),
                MathText(equation, textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                MathText(vertexLatex, textStyle: const TextStyle(fontSize: 14)),
              ],
            ),
          ),
        ),
        _SliderRow(label: 'a', value: _a, min: -5, max: 5, onChanged: (v) => setState(() => _a = v)),
        _SliderRow(label: 'b', value: _b, min: -10, max: 10, onChanged: (v) => setState(() => _b = v)),
        _SliderRow(label: 'c', value: _c, min: -10, max: 10, onChanged: (v) => setState(() => _c = v)),
      ],
    );
  }
}

class _QuadraticGraphPainter extends CustomPainter {
  final double a;
  final double b;
  final double c;

  _QuadraticGraphPainter({required this.a, required this.b, required this.c});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..color = Colors.black87..strokeWidth = 1;
    final gridPaint = Paint()..color = Colors.grey.withAlpha(50)..strokeWidth = 0.5;
    final graphPaint = Paint()..color = Colors.orange..strokeWidth = 2.5..style = PaintingStyle.stroke;

    final cx = size.width / 2;
    final cy = size.height / 2;
    const scale = 20.0;

    canvas.translate(cx, cy);

    canvas.drawLine(Offset(-size.width / 2, 0), Offset(size.width / 2, 0), paint);
    canvas.drawLine(Offset(0, -size.height / 2), Offset(0, size.height / 2), paint);

    for (double i = -(size.width / 2 / scale); i <= (size.width / 2 / scale); i += 1) {
      final x = i * scale;
      canvas.drawLine(Offset(x, -size.height / 2), Offset(x, size.height / 2), gridPaint);
    }
    for (double i = -(size.height / 2 / scale); i <= (size.height / 2 / scale); i += 1) {
      final y = i * scale;
      canvas.drawLine(Offset(-size.width / 2, y), Offset(size.width / 2, y), gridPaint);
    }

    final path = Path();
    bool started = false;

    for (double px = -size.width / 2; px <= size.width / 2; px += 2) {
      final val = a * (px / scale) * (px / scale) + b * (px / scale) + c;
      double py = -val * scale;
      if (!py.isFinite) continue;
      if (py > size.height / 2 + 50 || py < -size.height / 2 - 50) {
        started = false;
        continue;
      }
      py = py.clamp(-size.height / 2, size.height / 2);
      if (!started) {
        path.moveTo(px, py);
        started = true;
      } else {
        path.lineTo(px, py);
      }
    }

    canvas.drawPath(path, graphPaint);
  }

  @override
  bool shouldRepaint(covariant _QuadraticGraphPainter old) => old.a != a || old.b != b || old.c != c;
}

class TriangleSimulator extends StatefulWidget {
  const TriangleSimulator({super.key});

  @override
  State<TriangleSimulator> createState() => _TriangleSimulatorState();
}

class _TriangleSimulatorState extends State<TriangleSimulator> {
  double _sideA = 3;
  double _sideB = 4;
  double _sideC = 5;

  bool get _isValid {
    return _sideA + _sideB > _sideC && _sideA + _sideC > _sideB && _sideB + _sideC > _sideA;
  }

  double _angleA() {
    if (!_isValid) return 0;
    final cosA = (_sideB * _sideB + _sideC * _sideC - _sideA * _sideA) / (2 * _sideB * _sideC);
    return math.acos(cosA.clamp(-1.0, 1.0)) * 180 / math.pi;
  }

  double _angleB() {
    if (!_isValid) return 0;
    final cosB = (_sideA * _sideA + _sideC * _sideC - _sideB * _sideB) / (2 * _sideA * _sideC);
    return math.acos(cosB.clamp(-1.0, 1.0)) * 180 / math.pi;
  }

  double _angleC() {
    if (!_isValid) return 0;
    return 180 - _angleA() - _angleB();
  }

  double _area() {
    if (!_isValid) return 0;
    final s = (_sideA + _sideB + _sideC) / 2;
    return math.sqrt(s * (s - _sideA) * (s - _sideB) * (s - _sideC));
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Mô phỏng tam giác', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            SizedBox(
              height: 200,
              child: CustomPaint(
                size: const Size(double.infinity, 200),
                painter: _TrianglePainter(a: _sideA, b: _sideB, c: _sideC, valid: _isValid),
              ),
            ),
            const SizedBox(height: 16),
            _SliderRow(label: 'Cạnh a', value: _sideA, min: 1, max: 20, onChanged: (v) => setState(() => _sideA = v)),
            _SliderRow(label: 'Cạnh b', value: _sideB, min: 1, max: 20, onChanged: (v) => setState(() => _sideB = v)),
            _SliderRow(label: 'Cạnh c', value: _sideC, min: 1, max: 20, onChanged: (v) => setState(() => _sideC = v)),
            const SizedBox(height: 12),
            if (!_isValid)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text('Ba cạnh không thỏa mãn bất đẳng thức tam giác!', style: TextStyle(color: Colors.red, fontWeight: FontWeight.w600)),
              )
            else ...[
              const Divider(),
              const SizedBox(height: 8),
              MathText(r'\text{Góc A: } ' '${_angleA().toStringAsFixed(1)}^\circ', textStyle: const TextStyle(fontSize: 15)),
              const SizedBox(height: 4),
              MathText(r'\text{Góc B: } ' '${_angleB().toStringAsFixed(1)}^\circ', textStyle: const TextStyle(fontSize: 15)),
              const SizedBox(height: 4),
              MathText(r'\text{Góc C: } ' '${_angleC().toStringAsFixed(1)}^\circ', textStyle: const TextStyle(fontSize: 15)),
              const SizedBox(height: 8),
              MathText(r'\text{Diện tích: } ' '${_area().toStringAsFixed(2)}', textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold)),
            ],
          ],
        ),
      ),
    );
  }
}

class _TrianglePainter extends CustomPainter {
  final double a;
  final double b;
  final double c;
  final bool valid;

  _TrianglePainter({required this.a, required this.b, required this.c, required this.valid});

  @override
  void paint(Canvas canvas, Size size) {
    if (!valid) {
      final paint = Paint()..color = Colors.red..strokeWidth = 2..style = PaintingStyle.stroke;
      canvas.drawLine(Offset(size.width * 0.25, size.height * 0.75), Offset(size.width * 0.75, size.height * 0.75), paint);
      canvas.drawLine(Offset(size.width * 0.75, size.height * 0.75), Offset(size.width * 0.5, size.height * 0.25), paint);
      canvas.drawLine(Offset(size.width * 0.5, size.height * 0.25), Offset(size.width * 0.25, size.height * 0.75), paint);
      return;
    }

    final maxSide = [a, b, c].reduce(math.max);
    final scaleX = (size.width - 40) / maxSide;
    final scaleY = (size.height - 40) / maxSide;
    final scale = math.min(scaleX, scaleY);

    final sideA = a * scale;
    final sideB = b * scale;
    final sideC = c * scale;

    final cosB = (sideA * sideA + sideC * sideC - sideB * sideB) / (2 * sideA * sideC);
    final angleB = math.acos(cosB.clamp(-1.0, 1.0));

    final p1 = Offset(20, size.height - 20);
    final p2 = Offset(20 + sideA, size.height - 20);
    final p3 = Offset(20 + sideC * math.cos(angleB), size.height - 20 - sideC * math.sin(angleB));

    final paint = Paint()..color = Colors.teal..strokeWidth = 2.5..style = PaintingStyle.stroke;
    final fillPaint = Paint()..color = Colors.teal.withAlpha(30)..style = PaintingStyle.fill;

    final path = Path()..moveTo(p1.dx, p1.dy)..lineTo(p2.dx, p2.dy)..lineTo(p3.dx, p3.dy)..close();
    canvas.drawPath(path, fillPaint);
    canvas.drawPath(path, paint);

    final textPainter = TextPainter(textDirection: TextDirection.ltr);
    textPainter.text = TextSpan(text: 'a', style: const TextStyle(color: Colors.black87, fontSize: 14, fontWeight: FontWeight.bold));
    textPainter.layout();
    textPainter.paint(canvas, Offset((p1.dx + p2.dx) / 2 - textPainter.width / 2, p1.dy + 4));

    textPainter.text = TextSpan(text: 'b', style: const TextStyle(color: Colors.black87, fontSize: 14, fontWeight: FontWeight.bold));
    textPainter.layout();
    textPainter.paint(canvas, Offset((p2.dx + p3.dx) / 2 - textPainter.width / 2, (p2.dy + p3.dy) / 2 - textPainter.height - 4));

    textPainter.text = TextSpan(text: 'c', style: const TextStyle(color: Colors.black87, fontSize: 14, fontWeight: FontWeight.bold));
    textPainter.layout();
    textPainter.paint(canvas, Offset((p1.dx + p3.dx) / 2 - textPainter.width / 2, (p1.dy + p3.dy) / 2 - textPainter.height - 4));
  }

  @override
  bool shouldRepaint(covariant _TrianglePainter old) => old.a != a || old.b != b || old.c != c || old.valid != valid;
}
