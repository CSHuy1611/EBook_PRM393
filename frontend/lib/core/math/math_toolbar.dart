import 'package:flutter/material.dart';

class MathToolbar extends StatelessWidget {
  final void Function(String latex) onInsert;

  const MathToolbar({super.key, required this.onInsert});

  static const List<_ToolbarItem> _items = [
    _ToolbarItem(r'\frac{}{}', 'a/b', 'Phân số (Fraction)'),
    _ToolbarItem(r'^{}', 'xⁿ', 'Số mũ (Superscript)'),
    _ToolbarItem(r'_{}', 'xₙ', 'Chỉ số dưới (Subscript)'),
    _ToolbarItem(r'\sqrt{}', '√x', 'Căn bậc hai (Square Root)'),
    _ToolbarItem(r'\sqrt[n]{}', 'ⁿ√x', 'Căn bậc n (Nth Root)'),
    _ToolbarItem(r'\leq', '≤', 'Nhỏ hơn hoặc bằng'),
    _ToolbarItem(r'\geq', '≥', 'Lớn hơn hoặc bằng'),
    _ToolbarItem(r'\neq', '≠', 'Khác'),
    _ToolbarItem(r'\approx', '≈', 'Xấp xỉ'),
    _ToolbarItem(r'\equiv', '≡', 'Đồng nhất / Đồng dư'),
    _ToolbarItem(r'\pm', '±', 'Cộng trừ'),
    _ToolbarItem(r'\times', '×', 'Phép nhân'),
    _ToolbarItem(r'\div', '÷', 'Phép chia'),
    _ToolbarItem(r'\rightarrow', '→', 'Mũi tên phải'),
    _ToolbarItem(r'\leftarrow', '←', 'Mũi tên trái'),
    _ToolbarItem(r'\Rightarrow', '⇒', 'Suy ra'),
    _ToolbarItem(r'\Leftrightarrow', '⇔', 'Tương đương'),
    _ToolbarItem(r'\implies', '⟹', 'Kéo theo'),
    _ToolbarItem(r'\iff', '⟺', 'Khi và chỉ khi'),
    _ToolbarItem(r'\infty', '∞', 'Vô cực'),
    _ToolbarItem(r'\propto', '∝', 'Tỷ lệ thuận'),
    _ToolbarItem(r'\therefore', '∴', 'Do đó'),
    _ToolbarItem(r'\because', '∵', 'Bởi vì'),
    _ToolbarItem(r'\forall', '∀', 'Với mọi'),
    _ToolbarItem(r'\exists', '∃', 'Tồn tại'),
    _ToolbarItem(r'\in', '∈', 'Thuộc'),
    _ToolbarItem(r'\notin', '∉', 'Không thuộc'),
    _ToolbarItem(r'\subset', '⊂', 'Tập con'),
    _ToolbarItem(r'\supset', '⊃', 'Tập cha'),
    _ToolbarItem(r'\pi', 'π', 'Số Pi'),
    _ToolbarItem(r'\alpha', 'α', 'Góc Alpha'),
    _ToolbarItem(r'\beta', 'β', 'Góc Beta'),
    _ToolbarItem(r'\Delta', 'Δ', 'Delta'),
    _ToolbarItem(r'\triangle', '△', 'Tam giác'),
    _ToolbarItem(r'\angle', '∠', 'Góc'),
    _ToolbarItem(r'\perp', '⊥', 'Vuông góc'),
    _ToolbarItem(r'\parallel', '∥', 'Song song'),
    _ToolbarItem(r'^\circ', '°', 'Độ'),
  ];

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 44,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 4),
        child: Row(
          children: _items.map((item) {
            return Padding(
              padding: const EdgeInsets.only(right: 4),
              child: Tooltip(
                message: item.tooltip,
                child: OutlinedButton(
                  onPressed: () => onInsert(item.latex),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    minimumSize: const Size(36, 34),
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                    backgroundColor: Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(50),
                  ),
                  child: Text(
                    item.displayLabel,
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ),
                ),
              ),
            );
          }).toList(),
        ),
      ),
    );
  }
}

class _ToolbarItem {
  final String latex;
  final String displayLabel;
  final String tooltip;

  const _ToolbarItem(this.latex, this.displayLabel, this.tooltip);
}
