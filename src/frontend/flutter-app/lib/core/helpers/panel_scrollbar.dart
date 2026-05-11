import 'package:page_ui/config/themes/app_colors.dart';
import 'package:flutter/material.dart';

class PanelScrollbar extends StatelessWidget {
  const PanelScrollbar({
    super.key,
    required this.controller,
    required this.child,
  });

  final ScrollController controller;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final thumbColor = WidgetStateProperty.resolveWith<Color?>((states) {
      if (states.contains(WidgetState.dragged)) {
        return AppColors.primaryColor.withValues(alpha: 0.95);
      }
      if (states.contains(WidgetState.hovered)) {
        return AppColors.primaryColor.withValues(alpha: 0.82);
      }
      return AppColors.lightGray.withValues(alpha: 0.28);
    });

    final trackColor = WidgetStateProperty.resolveWith<Color?>((states) {
      final isActive =
          states.contains(WidgetState.dragged) ||
          states.contains(WidgetState.hovered);
      return AppColors.black.withValues(alpha: isActive ? 0.34 : 0.18);
    });

    final trackBorderColor = WidgetStateProperty.resolveWith<Color?>((states) {
      if (states.contains(WidgetState.dragged)) {
        return AppColors.white.withValues(alpha: 0.12);
      }
      return AppColors.white.withValues(alpha: 0.06);
    });

    return ScrollbarTheme(
      data: ScrollbarThemeData(
        thumbColor: thumbColor,
        trackColor: trackColor,
        trackBorderColor: trackBorderColor,
        thickness: const WidgetStatePropertyAll(8),
        radius: const Radius.circular(999),
        mainAxisMargin: 0,
        crossAxisMargin: 0,
        interactive: true,
        thumbVisibility: const WidgetStatePropertyAll(true),
        trackVisibility: const WidgetStatePropertyAll(true),
      ),
      child: Scrollbar(
        controller: controller,
        interactive: true,
        thumbVisibility: true,
        trackVisibility: true,
        child: child,
      ),
    );
  }
}
