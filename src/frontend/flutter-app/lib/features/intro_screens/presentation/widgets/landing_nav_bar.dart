import 'dart:ui';

import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/chat/presentation/widgets/name_and_the_logo.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/landing_view_nav_bar_item.dart';
import 'package:flutter/material.dart';

class LandingNavBar extends StatelessWidget {
  final ScrollController scrollController;
  final GlobalKey featuresKey;
  final GlobalKey aboutKey;
  final GlobalKey footerKey;

  const LandingNavBar({
    super.key,
    required this.scrollController,
    required this.featuresKey,
    required this.aboutKey,
    required this.footerKey,
  });

  void _scrollToSection(GlobalKey key) {
    final context = key.currentContext;
    if (context != null) {
      Scrollable.ensureVisible(
        context,
        duration: const Duration(milliseconds: 800),
        curve: Curves.easeInOutCubic,
      );
    }
  }

  void _scrollToTop() {
    scrollController.animateTo(
      0,
      duration: const Duration(milliseconds: 800),
      curve: Curves.easeInOutCubic,
    );
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final w = constraints.maxWidth;
        final compact = w < 600;

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
                  NameAndTheLogo(onTap: _scrollToTop),

                  
                  if (!compact)
                    Row(
                      children: [
                        LandingViewNavBarItem(
                          title: "Features",
                          onTap: () => _scrollToSection(featuresKey),
                        ),
                        const SizedBox(width: 24),
                        LandingViewNavBarItem(
                          title: "About",
                          onTap: () => _scrollToSection(aboutKey),
                        ),
                        const SizedBox(width: 24),
                        LandingViewNavBarItem(
                          title: "Contact",
                          onTap: () => _scrollToSection(footerKey),
                        ),
                      ],
                    ),

                  
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextButton(
                        onPressed: () => AppRoutes.goSplash(context),
                        child: Text(
                          "Log in",
                          style: AppTextStyles.titleMedium?.copyWith(
                            color: AppColors.white.withValues(alpha: 0.8),
                            fontSize: 15.0,
                          ),
                        ),
                      ),
                      
                      if (compact)
                        PopupMenuButton<String>(
                          icon: Icon(
                            Icons.menu,
                            color: AppColors.white.withValues(alpha: 0.8),
                            size: 22,
                          ),
                          color: AppColors.mainBackgroundColor,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                            side: BorderSide(
                              color: AppColors.white.withValues(alpha: 0.1),
                            ),
                          ),
                          onSelected: (value) {
                            switch (value) {
                              case 'features':
                                _scrollToSection(featuresKey);
                              case 'about':
                                _scrollToSection(aboutKey);
                              case 'contact':
                                _scrollToSection(footerKey);
                            }
                          },
                          itemBuilder: (_) => [
                            PopupMenuItem(
                              value: 'features',
                              child: Text(
                                'Features',
                                style: AppTextStyles.bodyMedium?.copyWith(
                                  color: AppColors.white,
                                ),
                              ),
                            ),
                            PopupMenuItem(
                              value: 'about',
                              child: Text(
                                'About',
                                style: AppTextStyles.bodyMedium?.copyWith(
                                  color: AppColors.white,
                                ),
                              ),
                            ),
                            PopupMenuItem(
                              value: 'contact',
                              child: Text(
                                'Contact',
                                style: AppTextStyles.bodyMedium?.copyWith(
                                  color: AppColors.white,
                                ),
                              ),
                            ),
                          ],
                        ),
                    ],
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
