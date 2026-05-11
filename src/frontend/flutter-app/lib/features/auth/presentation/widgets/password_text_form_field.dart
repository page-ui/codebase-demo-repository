import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/features/auth/presentation/widgets/password_validator.dart';
import 'package:flutter/material.dart';

class PasswordTextFormField extends StatefulWidget {
  const PasswordTextFormField({super.key, this.controller});
  final TextEditingController? controller;

  @override
  State<PasswordTextFormField> createState() => _PasswordTextFormFieldState();
}

class _PasswordTextFormFieldState extends State<PasswordTextFormField> {
  bool isSecure = true;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      validator: passwordValidator,
      controller: widget.controller,
      style: AppTextStyles.bodyMedium!.copyWith(color: AppColors.white),
      cursorColor: AppColors.primaryColor,
      mouseCursor: SystemMouseCursors.click,
      obscureText: isSecure,
      obscuringCharacter: "*",

      decoration: InputDecoration(
        prefixIcon: const Icon(AppIcons.arrowForward, size: 10),
        suffixIcon: IconButton(
          icon: Icon(
            isSecure ? AppIcons.visibilityOff : AppIcons.visibilityOn,
            color: AppColors.darkSurface,
          ),
          iconSize: 20,
          onPressed: () {
            setState(() {
              isSecure = !isSecure;
            });
          },
        ),
        enabledBorder: const OutlineInputBorder(
          borderRadius: AppBorders.xxxxs,
          borderSide: BorderSide(color: AppColors.darkSurface),
        ),
        focusedBorder: const OutlineInputBorder(
          borderRadius: AppBorders.xxxxs,
          borderSide: BorderSide(color: AppColors.cyan),
        ),
        errorBorder: const OutlineInputBorder(
          borderRadius: AppBorders.xxxxs,
          borderSide: BorderSide(color: AppColors.red),
        ),
        border: const OutlineInputBorder(
          borderRadius: AppBorders.xxxxs,
          borderSide: BorderSide(color: AppColors.darkGrey),
        ),
      ),
    );
  }
}
