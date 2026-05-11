import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:flutter/material.dart';

class CustomButton extends StatefulWidget {
  final VoidCallback? onPressed;
  final Widget? child;
  final String? title;

  const CustomButton({
    super.key,
    required this.onPressed,
    this.child,
    this.title,
  });

  @override
  State<CustomButton> createState() => _CustomButtonState();
}

class _CustomButtonState extends State<CustomButton> {
  bool _isPressed = false;
  void _setPressed(bool isPressed) {
    if (widget.onPressed != null) {
      setState(() => _isPressed = isPressed);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Listener(
      onPointerDown: (_) => _setPressed(true),
      onPointerUp: (_) => _setPressed(false),
      onPointerCancel: (_) => _setPressed(false),
      child: AnimatedScale(
        scale: _isPressed ? 0.95 : 1.0,
        duration: const Duration(milliseconds: 100),
        curve: Curves.easeInOut,
        child: TextButton(
          onPressed: widget.onPressed,
          style: const ButtonStyle(
            maximumSize: const WidgetStateProperty.fromMap({
              WidgetState.any: const Size(double.infinity, 50),
            }),
            minimumSize: const WidgetStateProperty.fromMap({
              WidgetState.any: const Size(double.infinity, 50),
            }),
            backgroundColor: const WidgetStateColor.fromMap({
              WidgetState.pressed: AppColors.primaryColor,
              WidgetState.any: AppColors.green,
            }),
            enableFeedback: true,
            shape: const WidgetStateProperty.fromMap({
              WidgetState.any: const RoundedRectangleBorder(
                borderRadius: AppBorders.xxxxs,
                side: BorderSide(color: AppColors.primaryColor),
              ),
            }),
          ),
          child:
              widget.child ??
              Text(
                "[ ${widget.title ?? ''} ]",
                style: AppTextStyles.bodyLarge!.copyWith(
                  color: AppColors.black,
                ),
              ),
        ),
      ),
    );
  }
}
