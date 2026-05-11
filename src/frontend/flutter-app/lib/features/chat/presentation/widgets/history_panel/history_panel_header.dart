import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/chat/presentation/widgets/custom_button_icon_for_panels.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/history_panel_body.dart';

class HistoryPanelHeader extends StatelessWidget {
  const HistoryPanelHeader({super.key, required this.widget});

  final HistoryPanelBody widget;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: AlignmentGeometry.topCenter,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            Text(
              "History Interface",
              style: AppTextStyles.bodyLarge!.copyWith(
                color: AppColors.white,
                letterSpacing: 2,
              ),
            ),
            CustomButtonIconForPanels(
              onPressed: widget.onPressed,
            ),
          ],
        ),
      ),
    );
  }
}
