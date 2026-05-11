import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/feature_card.dart';
import 'package:flutter/material.dart';

class FeaturesSection extends StatelessWidget {
  const FeaturesSection({super.key});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final w = constraints.maxWidth;
        final compact = w < 600;
        final medium = w >= 600 && w < 1100;

        final labelFont = compact ? 12.0 : 14.0;
        final headFont = compact
            ? 24.0
            : medium
            ? 32.0
            : 42.0;
        final titleFont = compact
            ? 18.0
            : medium
            ? 20.0
            : 22.0;
        final bodyFont = compact
            ? 14.0
            : medium
            ? 15.0
            : 16.0;
        final iconSize = compact
            ? 28.0
            : medium
            ? 30.0
            : 32.0;
        final hPad = compact
            ? 20.0
            : medium
            ? 48.0
            : 120.0;

        const cardSpacing = 24.0;
        final availableW = (w - hPad * 2)
            .clamp(0.0, double.infinity)
            .toDouble();
        final double cardW;
        if (compact) {
          cardW = availableW;
        } else if (medium) {
          cardW = ((availableW - cardSpacing) / 2)
              .clamp(0.0, double.infinity)
              .toDouble();
        } else {
          cardW = ((availableW - cardSpacing * 2) / 3)
              .clamp(0.0, double.infinity)
              .toDouble();
        }

        return Container(
          width: double.infinity,
          padding: EdgeInsets.symmetric(horizontal: hPad, vertical: 80),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                "Features",
                style: AppTextStyles.labelLarge?.copyWith(
                  color: AppColors.primaryColor,
                  letterSpacing: 2,
                  fontSize: labelFont,
                ),
              ),
              const SizedBox(height: 16),
              Text(
                "Built for the frontend developer",
                style: AppTextStyles.displayMedium?.copyWith(
                  color: AppColors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: headFont,
                ),
              ),
              const SizedBox(height: 48),
              Wrap(
                spacing: cardSpacing,
                runSpacing: 24,
                children: [
                  FeatureCard(
                    icon: Icons.lightbulb_outline,
                    title: "Beyond Rigid Templates",
                    description:
                        "Unlike traditional AI tools, we learn from real-world design patterns (like Pinterest) to encourage diversity and avoid repetitive, generic interfaces.",
                    cardWidth: cardW,
                    titleFont: titleFont,
                    bodyFont: bodyFont,
                    iconSize: iconSize,
                  ),
                  FeatureCard(
                    icon: Icons.code,
                    title: "Developer Centric",
                    description:
                        "A design support system for you. Translate abstract ideas into concrete UI outputs instantly, freeing you to focus on logic and state management.",
                    cardWidth: cardW,
                    titleFont: titleFont,
                    bodyFont: bodyFont,
                    iconSize: iconSize,
                  ),
                  FeatureCard(
                    icon: Icons.dashboard_customize,
                    title: "Originality & Adaptability",
                    description:
                        "Focuses on structural and aesthetic design principles to ensure every generated UI is unique, tailored to your intent, and easy to refine.",
                    cardWidth: cardW,
                    titleFont: titleFont,
                    bodyFont: bodyFont,
                    iconSize: iconSize,
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}
