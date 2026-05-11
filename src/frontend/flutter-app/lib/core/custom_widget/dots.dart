import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:flutter/material.dart';

class Dots extends StatelessWidget {
  Dots({super.key});
  final double size = 10;
  final Color color = AppColors.darkGrey;
  final IconData icon = AppIcons.dot;
  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, color: color, size: size),
        const SizedBox(width: 4),
        Icon(icon, color: color, size: size),
        const SizedBox(width: 4),
        Icon(icon, color: color, size: size),
      ],
    );
  }
}
