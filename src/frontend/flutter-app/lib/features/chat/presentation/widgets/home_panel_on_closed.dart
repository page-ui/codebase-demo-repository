import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/chat/presentation/widgets/custom_button_icon_for_panels.dart';
import 'package:flutter/material.dart';

class HomePanelOnClosed extends StatelessWidget {
  const HomePanelOnClosed({
    super.key,
    required this.onPressed,
    required this.isOpen,
  });

  final void Function()? onPressed;
  final bool isOpen;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        CustomButtonIconForPanels(
          onPressed: onPressed,
        ),
        const Spacer(flex: 1),

        AnimatedOpacity(
          opacity: isOpen ? 0 : 1.0,
          duration: const Duration(milliseconds: 400),
          curve: Curves.easeIn,

          child: AnimatedScale(
            scale: isOpen ? 1.0 : 0.7,
            duration: const Duration(milliseconds: 500),
            curve: Curves.decelerate,

            child: AnimatedRotation(
              turns: isOpen ? 0.25 : 0.0,
              duration: const Duration(milliseconds: 500),
              curve: Curves.decelerate,

              child: RotatedBox(
                quarterTurns: 1,
                child: Text(
                  "Chat" ,
                  style: AppTextStyles.headlineSmall!.copyWith(
                    letterSpacing: 8,
                    color: AppColors.primaryColor,
                  ),
                ),
              ),
            ),
          ),
        ),
        const Spacer(flex: 2),
      ],
    );
  }
}
