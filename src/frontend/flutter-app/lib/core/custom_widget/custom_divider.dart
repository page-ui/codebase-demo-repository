import 'package:page_ui/config/themes/app_colors.dart';
import 'package:flutter/material.dart';

class CustomDivider extends StatelessWidget {
  const CustomDivider({super.key});

  @override
  Widget build(BuildContext context) {
    return Divider(color: AppColors.primaryColor.withValues(alpha: 0.3));
  }
}
