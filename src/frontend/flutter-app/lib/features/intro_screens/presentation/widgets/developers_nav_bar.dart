import 'dart:ui';

import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/chat/presentation/widgets/name_and_the_logo.dart';
import 'package:flutter/material.dart';

class DevelopersNavBar extends StatelessWidget {
  const DevelopersNavBar({super.key});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 600;

        return ClipRRect(
          child: BackdropFilter(
            filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
            child: Container(
              width: double.infinity,
              padding: EdgeInsets.symmetric(
                horizontal: compact ? 12 : 24,
                vertical: 14,
              ),
              decoration: BoxDecoration(
                color: AppColors.mainBackgroundColor.withValues(alpha: 0.5),
                border: Border(
                  bottom: BorderSide(
                    color: AppColors.white.withValues(alpha: 0.1),
                    width: 1,
                  ),
                ),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  NameAndTheLogo(
                    onTap: () => AppRoutes.goLanding(context),
                  ),
                  TextButton(
                    onPressed: () => AppRoutes.goLogin(context),
                    child: Text(
                      'Log in',
                      style: AppTextStyles.titleMedium?.copyWith(
                        color: AppColors.white.withValues(alpha: 0.8),
                        fontSize: 15.0,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}
