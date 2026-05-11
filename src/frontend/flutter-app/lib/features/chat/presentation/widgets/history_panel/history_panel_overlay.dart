import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

class HistoryPanelOverlay extends StatelessWidget {
  const HistoryPanelOverlay({
    super.key,
    required this.width,
    required this.panel,
    required this.onClose,
  });

  final double width;
  final Widget panel;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        PointerInterceptor(
          child: GestureDetector(
            onTap: onClose,
            child: Container(
              decoration: BoxDecoration(
                color: AppColors.black.withValues(alpha: 0.7),
              ),
            ),
          ),
        ),
        Positioned(
          left: 0,
          top: 0,
          bottom: 0,
          child: PointerInterceptor(
            child: Container(
              width: width,
              decoration: BoxDecoration(
                borderRadius: const BorderRadius.only(
                  topRight: Radius.circular(8),
                  bottomRight: Radius.circular(8),
                ),
                color: AppColors.anotherGray.withValues(alpha: 0.95),
                border: Border.all(color: AppColors.darkGrey, width: 0.5),
              ),
              clipBehavior: Clip.antiAlias,
              child: ColoredBox(color: AppColors.transparent, child: panel),
            ),
          ),
        ),
      ],
    );
  }
}
