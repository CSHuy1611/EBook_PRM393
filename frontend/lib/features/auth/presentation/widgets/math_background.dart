import 'package:flutter/material.dart';

/// Background widget hiển thị lưới ô ly + công thức toán học rõ nét
/// Dùng chung cho Login và Register screens
class MathBackground extends StatelessWidget {
  final Widget child;

  const MathBackground({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Stack(
      children: [
        // Lớp 1: Nền gradient nhẹ
        Positioned.fill(
          child: Container(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  const Color(0xFFF0EDFF), // Lavender nhạt
                  const Color(0xFFFFF8E7), // Vàng kem nhạt
                  const Color(0xFFE8F5E9), // Xanh lá nhạt
                ],
                stops: const [0.0, 0.5, 1.0],
              ),
            ),
          ),
        ),

        // Lớp 2: Lưới ô ly vở toán
        Positioned.fill(
          child: CustomPaint(
            painter: _GridPainter(color: colorScheme.primary.withOpacity(0.06)),
          ),
        ),

        // Lớp 3: Công thức toán HIỂN THỊ RÕ
        const Positioned(top: 30, left: 30, child: _MathFormula(text: 'y = ax + b', size: 20)),
        const Positioned(top: 35, right: 40, child: _MathFormula(text: 'a² + b² = c²', size: 22)),
        const Positioned(top: 120, left: 60, child: _MathFormula(text: '∫ f(x)dx', size: 18)),
        const Positioned(top: 100, right: 80, child: _MathFormula(text: 'Δ = b² − 4ac', size: 17)),
        const Positioned(bottom: 40, left: 30, child: _MathFormula(text: '(a+b)² = a² + 2ab + b²', size: 18)),
        const Positioned(bottom: 35, right: 30, child: _MathFormula(text: 'S = πr²', size: 22)),
        const Positioned(bottom: 110, left: 50, child: _MathFormula(text: 'sin²α + cos²α = 1', size: 16)),
        const Positioned(bottom: 120, right: 50, child: _MathFormula(text: 'f(x) = x³ − 3x', size: 17)),
        Positioned(top: 220, left: 15, child: _MathFormula(text: 'x = (−b ± √Δ) / 2a', size: 15)),
        Positioned(top: 200, right: 20, child: _MathFormula(text: 'V = ⅓πr²h', size: 18)),
        Positioned(bottom: 220, left: 20, child: _MathFormula(text: '∑ⁿ k = n(n+1)/2', size: 16)),
        Positioned(bottom: 200, right: 25, child: _MathFormula(text: 'log₂8 = 3', size: 18)),

        // Lớp 4: Nội dung chính
        Positioned.fill(child: child),
      ],
    );
  }
}

/// Widget hiển thị 1 công thức toán – rõ nét, font serif đẹp
class _MathFormula extends StatelessWidget {
  final String text;
  final double size;

  const _MathFormula({required this.text, this.size = 16});

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: TextStyle(
        fontSize: size,
        fontWeight: FontWeight.w600,
        fontStyle: FontStyle.italic,
        fontFamily: 'serif',
        color: Theme.of(context).colorScheme.primary.withOpacity(0.15),
        letterSpacing: 1.2,
      ),
    );
  }
}

/// Vẽ lưới ô ly vở toán
class _GridPainter extends CustomPainter {
  final Color color;
  _GridPainter({required this.color});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 0.5;

    const step = 28.0;
    for (double x = 0; x < size.width; x += step) {
      canvas.drawLine(Offset(x, 0), Offset(x, size.height), paint);
    }
    for (double y = 0; y < size.height; y += step) {
      canvas.drawLine(Offset(0, y), Offset(size.width, y), paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
