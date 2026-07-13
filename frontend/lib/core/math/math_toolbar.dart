import 'package:flutter/material.dart';

class MathToolbar extends StatelessWidget {
  final void Function(String latex) onInsert;

  const MathToolbar({super.key, required this.onInsert});

  static const List<_ToolbarItem> _items = [
    _ToolbarItem(r'\frac{}{}', 'Fraction'),
    _ToolbarItem(r'^{}', 'Superscript'),
    _ToolbarItem(r'_{}', 'Subscript'),
    _ToolbarItem(r'\sqrt{}', 'Square Root'),
    _ToolbarItem(r'\sqrt[n]{}', 'Nth Root'),
    _ToolbarItem(r'\leq', '≤'),
    _ToolbarItem(r'\geq', '≥'),
    _ToolbarItem(r'\neq', '≠'),
    _ToolbarItem(r'\approx', '≈'),
    _ToolbarItem(r'\equiv', '≡'),
    _ToolbarItem(r'\pm', '±'),
    _ToolbarItem(r'\times', '×'),
    _ToolbarItem(r'\div', '÷'),
    _ToolbarItem(r'\rightarrow', '→'),
    _ToolbarItem(r'\leftarrow', '←'),
    _ToolbarItem(r'\Rightarrow', '⇒'),
    _ToolbarItem(r'\Leftrightarrow', '⇔'),
    _ToolbarItem(r'\implies', '⟹'),
    _ToolbarItem(r'\iff', '⟺'),
    _ToolbarItem(r'\infty', '∞'),
    _ToolbarItem(r'\propto', '∝'),
    _ToolbarItem(r'\therefore', '∴'),
    _ToolbarItem(r'\because', '∵'),
    _ToolbarItem(r'\forall', '∀'),
    _ToolbarItem(r'\exists', '∃'),
    _ToolbarItem(r'\in', '∈'),
    _ToolbarItem(r'\notin', '∉'),
    _ToolbarItem(r'\subset', '⊂'),
    _ToolbarItem(r'\supset', '⊃'),
    _ToolbarItem(r'\pi', 'π'),
    _ToolbarItem(r'\alpha', 'α'),
    _ToolbarItem(r'\beta', 'β'),
    _ToolbarItem(r'\Delta', 'Δ'),
    _ToolbarItem(r'\triangle', '△'),
    _ToolbarItem(r'\angle', '∠'),
    _ToolbarItem(r'\perp', '⊥'),
    _ToolbarItem(r'\parallel', '∥'),
    _ToolbarItem(r'^\circ', '°'),
  ];

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 48,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 8),
        child: Row(
          children: _items.map((item) {
            return Padding(
              padding: const EdgeInsets.only(right: 4),
              child: TextButton(
                onPressed: () => onInsert(item.latex),
                style: TextButton.styleFrom(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                  minimumSize: const Size(28, 28),
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(6),
                    side: BorderSide(
                      color: Theme.of(context).colorScheme.outline.withAlpha(76),
                    ),
                  ),
                ),
                child: Text(
                  item.label,
                  style: const TextStyle(
                    fontSize: 12,
                    fontFamily: 'monospace',
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
  final String tooltip;

  const _ToolbarItem(this.latex, this.tooltip);

  String get label {
    if (latex == r'\sqrt[n]{}') return r'\sqrt[n]';
    return latex;
  }
}
