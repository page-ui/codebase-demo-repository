import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/developer_profile.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/developer_section.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/developers_nav_bar.dart';

class DevelopersView extends StatelessWidget {
  static const String routeName = 'DevelopersView';

  const DevelopersView({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: LayoutBuilder(
        builder: (context, constraints) {
          final width = constraints.maxWidth;
          final compact = width < 600;
          final hPad = compact ? 16.0 : 48.0;
          final titleFont = compact ? 22.0 : 28.0;
          const bodyFont = 15.0;
          final cardTitleFont = compact ? 18.0 : 20.0;
          final nameFont = compact ? 15.0 : 16.0;
          final repoFont = compact ? 12.0 : 13.0;

          return Stack(
            children: [
              SingleChildScrollView(
                child: Padding(
                  padding: EdgeInsets.fromLTRB(hPad, 80, hPad, 48),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Developers',
                        style: AppTextStyles.headlineMedium?.copyWith(
                          color: AppColors.white,
                          fontSize: titleFont,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        'Frontend, backend, and AI contributors with direct links to their GitHub profiles.',
                        style: AppTextStyles.titleMedium?.copyWith(
                          color: AppColors.white.withValues(alpha: 0.6),
                          fontSize: bodyFont,
                        ),
                      ),
                      const SizedBox(height: 32),
                      DeveloperSection(
                        title: 'Frontend Developer',
                        accent: AppColors.lightCyan,
                        titleFont: cardTitleFont,
                        nameFont: nameFont,
                        repoFont: repoFont,
                        developers: const [
                          DeveloperProfile(
                            name: 'Abdelrahman Khaled',
                            repoUrl: 'https://github.com/Polymath000',
                            linkedin: 'https://www.linkedin.com/in/polymath00/',
                          ),
                        ],
                      ),
                      const SizedBox(height: 20),
                      DeveloperSection(
                        title: 'Backend Developer',
                        accent: AppColors.greenAccent,
                        titleFont: cardTitleFont,
                        nameFont: nameFont,
                        repoFont: repoFont,
                        developers: const [
                          DeveloperProfile(
                            name: 'Mohamed Alaa',
                            repoUrl: 'https://github.com/Anubisx404',
                            linkedin:
                                'https://www.linkedin.com/in/mohamed-alaa-231b972a5/',
                          ),
                        ],
                      ),
                      const SizedBox(height: 20),
                      DeveloperSection(
                        title: 'AI Developers',
                        accent: AppColors.amber,
                        titleFont: cardTitleFont,
                        nameFont: nameFont,
                        repoFont: repoFont,
                        developers: const [
                          DeveloperProfile(
                            name: 'Abdelrahman Abdelnaser',
                            repoUrl: 'https://github.com/abdelrhmannaser845',
                            linkedin:
                                'https://www.linkedin.com/in/abdelrhman-naser-4480a8285/',
                          ),
                          DeveloperProfile(
                            name: 'Zeyad Alaa',
                            repoUrl: 'https://github.com/zeyad-alaa00',
                            linkedin:
                                'https://www.linkedin.com/in/zeyad-alaa-166146388/',
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
              const Positioned(
                top: 0,
                left: 0,
                right: 0,
                child: DevelopersNavBar(),
              ),
            ],
          );
        },
      ),
    );
  }
}
