import 'package:animated_text_kit/animated_text_kit.dart';
import 'package:flutter/material.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/constants.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/features/chat/presentation/widgets/ui_frame/iframe_view.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/read_docs_button.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

class HeroSection extends StatelessWidget {
  const HeroSection({super.key});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final w = constraints.maxWidth;
        final compact = context.isMobile;
        final medium = context.isTablet;

        final heroFont = compact
            ? 26.0
            : medium
            ? 36.0
            : 48.0;
        final bodyFont = compact
            ? 14.0
            : medium
            ? 17.0
            : 20.0;
        final pillFont = compact
            ? 11.0
            : medium
            ? 13.0
            : 14.0;
        final btnFont = compact
            ? 14.0
            : medium
            ? 16.0
            : 18.0;
        final hPad = compact
            ? 20.0
            : medium
            ? 48.0
            : 120.0;
        final pillIconSize = compact
            ? 14.0
            : medium
            ? 16.0
            : 18.0;

        return Container(
          width: double.infinity,
          padding: EdgeInsets.symmetric(horizontal: hPad),
          child: Column(
            children: [
              const SizedBox(height: 90),

              
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 18,
                  vertical: 10,
                ),
                decoration: BoxDecoration(
                  color: AppColors.white.withValues(alpha: 0.05),
                  borderRadius: BorderRadius.circular(30),
                  border: Border.all(
                    color: AppColors.white.withValues(alpha: 0.1),
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.auto_awesome,
                      color: AppColors.primaryColor,
                      size: pillIconSize,
                    ),
                    const SizedBox(width: 10),
                    Flexible(
                      child: Text(
                        "Introducing Prompt-to-UI Generation",
                        style: AppTextStyles.labelMedium?.copyWith(
                          color: AppColors.white,
                          fontSize: pillFont,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 26),

              DefaultTextStyle(
                style: AppTextStyles.displayLarge!.copyWith(
                  color: AppColors.white,
                  fontSize: heroFont,
                  fontWeight: FontWeight.bold,
                  height: 1.2,
                  letterSpacing: -1,
                ),
                textAlign: TextAlign.center,
                child: AnimatedTextKit(
                  repeatForever: true,
                  pause: const Duration(milliseconds: 3000),
                  animatedTexts: [
                    TypewriterAnimatedText(
                      appDescription,
                      speed: const Duration(milliseconds: 100),
                      textStyle: AppTextStyles.displayMedium!.copyWith(
                        fontSize: heroFont,
                        color: AppColors.white,
                        fontWeight: FontWeight.bold,
                        height: 1.2,
                        letterSpacing: -1,
                      ),
                      cursor: '|',
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              
              ConstrainedBox(
                constraints: BoxConstraints(maxWidth: compact ? w * 0.95 : 700),
                child: Text(
                  "Empowering frontend developers to build visually coherent, original UI designs instantly from natural language.",
                  textAlign: TextAlign.center,
                  style: AppTextStyles.headlineSmall?.copyWith(
                    color: AppColors.white.withValues(alpha: 0.7),
                    fontSize: bodyFont,
                    height: 1.6,
                  ),
                ),
              ),
              const SizedBox(height: 40),

              
              Wrap(
                alignment: WrapAlignment.center,
                spacing: 20,
                runSpacing: 16,
                children: [
                  ElevatedButton(
                    onPressed: () => AppRoutes.goLogin(context),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.green,
                      foregroundColor: AppColors.black,
                      padding: EdgeInsets.symmetric(
                        horizontal: compact ? 28 : 40,
                        vertical: compact ? 14 : 18,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(40),
                      ),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          "Start Building",
                          style: AppTextStyles.titleLarge?.copyWith(
                            color: AppColors.black,
                            fontWeight: FontWeight.bold,
                            fontSize: btnFont,
                          ),
                        ),
                        const SizedBox(width: 10),
                        Icon(
                          Icons.arrow_forward,
                          size: btnFont,
                          color: AppColors.black,
                        ),
                      ],
                    ),
                  ),
                  ReadDocsButton(btnFont: btnFont, compact: compact),
                ],
              ),

              const SizedBox(height: 60),

              
              PointerInterceptor(
                child: Container(
                  width: compact
                      ? double.infinity
                      : (w * 1.5).clamp(400.0, 900.0),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [
                        AppColors.white.withValues(alpha: 0.05),
                        AppColors.anotherGray,
                      ],
                    ),
                    borderRadius: const BorderRadius.all(Radius.circular(20)),
                    border: Border(
                      bottom: BorderSide(
                        color: AppColors.white.withValues(alpha: 0.1),
                      ),
                      top: BorderSide(
                        color: AppColors.white.withValues(alpha: 0.1),
                      ),
                      left: BorderSide(
                        color: AppColors.white.withValues(alpha: 0.1),
                      ),
                      right: BorderSide(
                        color: AppColors.white.withValues(alpha: 0.1),
                      ),
                    ),
                  ),
                  child: const AspectRatio(
                    aspectRatio: 3 / 1,
                    child: ClipRRect(
                      borderRadius: BorderRadius.vertical(
                        top: Radius.circular(20),
                      ),
                      child: IframeView(url: 'htmlFiles/index.html'),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 40),
            ],
          ),
        );
      },
    );
  }
}
