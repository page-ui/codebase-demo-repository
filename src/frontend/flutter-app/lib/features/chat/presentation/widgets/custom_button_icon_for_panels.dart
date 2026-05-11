import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

class CustomButtonIconForPanels extends StatelessWidget {
  const CustomButtonIconForPanels({
    super.key,
    required this.onPressed,
  });

  final void Function()? onPressed;

  @override
  Widget build(BuildContext context) {
    return PointerInterceptor(
      child: IconButton(
        padding: const EdgeInsets.all(0),
        onPressed: onPressed,
        icon: const Icon(AppIcons.swapping, color: AppColors.grey, size: 12),
      ),
    );
  }
}
