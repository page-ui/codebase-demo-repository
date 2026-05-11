import 'dart:ui';

import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:flutter/material.dart';

class FeatureCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final String description;
  final double cardWidth;
  final double titleFont;
  final double bodyFont;
  final double iconSize;

  const FeatureCard({
    required this.icon,
    required this.title,
    required this.description,
    required this.cardWidth,
    required this.titleFont,
    required this.bodyFont,
    required this.iconSize,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: cardWidth,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            blurStyle: BlurStyle.outer,
            color: AppColors.green.withValues(alpha: 0.2),
            blurRadius: 20,
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(24),
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
          child: Container(
            padding: const EdgeInsets.all(28),
            decoration: BoxDecoration(
              color: AppColors.white.withValues(alpha: 0.03),
              borderRadius: BorderRadius.circular(24),
              border: Border.all(
                color: AppColors.white.withValues(alpha: 0.05),
              ),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppColors.white.withValues(alpha: 0.05),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(icon, color: AppColors.white, size: iconSize),
                ),
                const SizedBox(height: 24),
                Text(
                  title,
                  style: AppTextStyles.headlineSmall?.copyWith(
                    color: AppColors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: titleFont,
                  ),
                ),
                const SizedBox(height: 14),
                Text(
                  description,
                  style: AppTextStyles.titleMedium?.copyWith(
                    color: AppColors.white.withValues(alpha: 0.6),
                    height: 1.5,
                    fontSize: bodyFont,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
