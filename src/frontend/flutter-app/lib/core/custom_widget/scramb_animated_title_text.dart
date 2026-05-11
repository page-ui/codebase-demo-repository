import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:animated_text_kit/animated_text_kit.dart';
import 'package:flutter/material.dart';

class ScrambAnimatedTitleText extends StatelessWidget {
  const ScrambAnimatedTitleText({super.key, required this.viewTitle});

  final String viewTitle;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 32,
      child: DefaultTextStyle(
        style: AppTextStyles.titleLarge!.copyWith(
          color: AppColors.primaryColor,
        ),
        child: AnimatedTextKit(
          repeatForever: true,
          animatedTexts: [
            ScrambleAnimatedText(
              viewTitle,
              speed: const Duration(milliseconds: 350),
            ),
          ],
        ),
        textAlign: TextAlign.center,
      ),
    );
  }
}
