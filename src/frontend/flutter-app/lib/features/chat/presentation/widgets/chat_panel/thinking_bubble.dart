import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:flutter/material.dart';

class ThinkingBubble extends StatefulWidget {
  const ThinkingBubble({super.key, required this.label});

  final String label;

  @override
  State<ThinkingBubble> createState() => _ThinkingBubbleState();
}

class _ThinkingBubbleState extends State<ThinkingBubble>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    )..repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        constraints: const BoxConstraints(maxWidth: 310),
        margin: const EdgeInsets.symmetric(vertical: 4),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: AppColors.anotherGray.withValues(alpha: 0.6),
          borderRadius: AppBorders.xxxxs,
          border: Border.all(
            color: AppColors.darkGrey.withValues(alpha: 0.5),
            width: 0.5,
          ),
        ),
        child: AnimatedBuilder(
          animation: _controller,
          builder: (context, _) {
            return Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Flexible(
                  child: Text(
                    widget.label,
                    style: TextStyle(
                      color: AppColors.white.withValues(alpha: 0.9),
                      fontSize: 14,
                      height: 1.5,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                const SizedBox(width: 4),
                _Dot(phase: _controller.value, index: 0),
                _Dot(phase: _controller.value, index: 1),
                _Dot(phase: _controller.value, index: 2),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _Dot extends StatelessWidget {
  const _Dot({required this.phase, required this.index});

  final double phase;
  final int index;

  @override
  Widget build(BuildContext context) {
    final offset = (phase + index * 0.2) % 1.0;
    final t = (offset < 0.5 ? offset * 2 : (1 - offset) * 2).clamp(0.0, 1.0);
    final alpha = 0.3 + t * 0.6;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 1.5),
      child: Text(
        '.',
        style: TextStyle(
          color: AppColors.white.withValues(alpha: alpha),
          fontSize: 18,
          height: 1,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
