import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:flutter/material.dart';

class AuthTextFormField extends StatelessWidget {
  const AuthTextFormField({
    super.key,
    this.controller,
    required this.validator,
    this.enable = true,
  });
  final bool enable;
  final TextEditingController? controller;
  final FormFieldValidator<String> validator;
  @override
  Widget build(BuildContext context) {
    return TextFormField(
      enabled: enable,
      controller: controller,
      validator: validator,
      cursorColor: AppColors.primaryColor,
      style: AppTextStyles.bodyMedium!.copyWith(color: AppColors.white),
      mouseCursor: SystemMouseCursors.click,
      decoration: CustomInputDecorationForTextField(),
    );
  }

  InputDecoration CustomInputDecorationForTextField() {
    return const InputDecoration(
      prefixIcon: Icon(AppIcons.arrowForward, size: 10),
      disabledBorder: OutlineInputBorder(
        borderRadius: AppBorders.xxxxs,
        borderSide: BorderSide(color: AppColors.lightAmber),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: AppBorders.xxxxs,
        borderSide: BorderSide(color: AppColors.darkSurface),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: AppBorders.xxxxs,
        borderSide: BorderSide(color: AppColors.cyan),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: AppBorders.xxxxs,
        borderSide: BorderSide(color: AppColors.red),
      ),
      border: OutlineInputBorder(
        borderRadius: AppBorders.xxxxs,
        borderSide: BorderSide(color: AppColors.darkGrey),
      ),
    );
  }
}
