import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:flutter/material.dart';

class customRowAuth extends StatelessWidget {
  const customRowAuth({super.key, required this.hint});
  final String hint;
  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        const Icon(
          AppIcons.arrowForward,
          color: AppColors.primaryColor,
          size: 12,
        ),
        Text(
          " $hint:",
          style: AppTextStyles.bodyMedium!.copyWith(
            color: AppColors.primaryColor,
          ),
        ),
      ],
    );
  }
}
