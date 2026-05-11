import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/core/enum/screen_type.dart';

void showSnackBar({
  required BuildContext context,
  required String message,
  Duration duration = const Duration(seconds: 6),
  Color backgroundColor = AppColors.black,
  Color textColor = AppColors.primaryColor,
  IconData? icon,
}) {
  final isMobileScreen = context.isMobile;
  final platform = Theme.of(context).platform;
  final isMobilePlatform =
      platform == TargetPlatform.android || platform == TargetPlatform.iOS;

  if (!kIsWeb || isMobileScreen || isMobilePlatform) {
    final messenger = ScaffoldMessenger.maybeOf(context);
    if (messenger == null) return;

    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Row(
            children: [
              if (icon != null) ...[
                Icon(icon, color: textColor),
                const SizedBox(width: 12),
              ],
              Expanded(
                child: Text(
                  message,
                  style: AppTextStyles.bodyMedium?.copyWith(color: textColor),
                ),
              ),
              const SizedBox(width: 8),
              GestureDetector(
                onTap: messenger.hideCurrentSnackBar,
                child: Icon(Icons.close, size: 18, color: textColor),
              ),
            ],
          ),
          duration: duration,
          backgroundColor: backgroundColor,
          behavior: SnackBarBehavior.floating,
          elevation: 12,
          dismissDirection: DismissDirection.horizontal,
          margin: const EdgeInsets.all(16),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: AppBorders.xxxxs,
            side: BorderSide(color: textColor.withValues(alpha: 0.5)),
          ),
        ),
      );
    return;
  }

  final overlay = Overlay.of(context);
  late OverlayEntry overlayEntry;

  overlayEntry = OverlayEntry(
    builder: (context) {
      return _WebToastWidget(
        message: message,
        backgroundColor: backgroundColor,
        textColor: textColor,
        icon: icon,
        onDismiss: () {
          overlayEntry.remove();
        },
      );
    },
  );

  overlay.insert(overlayEntry);

  Future.delayed(duration, () {
    if (overlayEntry.mounted) {
      overlayEntry.remove();
    }
  });
}

class _WebToastWidget extends StatefulWidget {
  final String message;
  final Color backgroundColor;
  final Color textColor;
  final IconData? icon;
  final VoidCallback onDismiss;

  const _WebToastWidget({
    required this.message,
    required this.backgroundColor,
    required this.textColor,
    required this.onDismiss,
    this.icon,
  });

  @override
  State<_WebToastWidget> createState() => _WebToastWidgetState();
}

class _WebToastWidgetState extends State<_WebToastWidget>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<Offset> _slide;
  late Animation<double> _fade;

  @override
  void initState() {
    super.initState();

    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 350),
    );

    _slide = Tween<Offset>(
      begin: const Offset(1, 0),
      end: Offset.zero,
    ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));

    _fade = Tween<double>(begin: 0, end: 1).animate(_controller);

    _controller.forward();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 30,
      right: 30,
      child: SlideTransition(
        position: _slide,
        child: FadeTransition(
          opacity: _fade,
          child: Material(
            color: AppColors.transparent,
            child: Container(
              width: 360,
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
              decoration: BoxDecoration(
                color: widget.backgroundColor,
                borderRadius: AppBorders.xxxxs,
                boxShadow: [
                  BoxShadow(
                    color: AppColors.black.withValues(alpha: 0.3),
                    blurRadius: 20,
                  ),
                ],
                border: Border.all(
                  color: widget.textColor.withValues(alpha: 0.5),
                ),
              ),
              child: Row(
                children: [
                  if (widget.icon != null) ...[
                    Icon(widget.icon, color: widget.textColor),
                    const SizedBox(width: 12),
                  ],
                  Expanded(
                    child: Text(
                      widget.message,
                      style: AppTextStyles.bodyMedium!.copyWith(
                        color: widget.textColor,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  GestureDetector(
                    onTap: widget.onDismiss,
                    child: Icon(Icons.close, size: 18, color: widget.textColor),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
