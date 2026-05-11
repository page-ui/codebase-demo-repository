import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/constants.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/footer_links_column.dart';
import 'package:flutter/material.dart';

class FooterSection extends StatelessWidget {
  const FooterSection({super.key});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final w = constraints.maxWidth;
        final compact = w < 600;
        final medium = w >= 600 && w < 1100;

        final headFont = compact
            ? 18.0
            : medium
            ? 20.0
            : 22.0;
        final bodyFont = compact
            ? 14.0
            : medium
            ? 15.0
            : 16.0;
        final linkFont = compact
            ? 13.0
            : medium
            ? 14.0
            : 15.0;
        final smallFont = compact ? 11.0 : 13.0;
        final hPad = compact
            ? 20.0
            : medium
            ? 48.0
            : 120.0;

        return Container(
          width: double.infinity,
          padding: EdgeInsets.symmetric(horizontal: hPad, vertical: 48),
          decoration: BoxDecoration(
            color: AppColors.black.withValues(alpha: 0.5),
            border: Border(
              top: BorderSide(color: AppColors.white.withValues(alpha: 0.1)),
            ),
          ),
          child: Column(
            children: [
              compact
                  ? _buildCompactFooter(headFont, bodyFont, linkFont)
                  : _buildWideFooter(headFont, bodyFont, linkFont),

              const SizedBox(height: 40),
              Divider(color: AppColors.white.withValues(alpha: 0.1)),
              const SizedBox(height: 16),

              Align(
                alignment: Alignment.centerLeft,
                child: Text(
                  "© ${DateTime.now().year} Page.ui.",
                  style: AppTextStyles.labelMedium?.copyWith(
                    color: AppColors.white.withValues(alpha: 0.4),
                    fontSize: smallFont,
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildWideFooter(double headFont, double bodyFont, double linkFont) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          flex: 2,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                appName,
                style: AppTextStyles.headlineMedium?.copyWith(
                  color: AppColors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: headFont,
                ),
              ),
              const SizedBox(height: 14),
              Text(
                "Empowering frontend developers to build visually coherent, original UI designs instantly.",
                style: AppTextStyles.titleMedium?.copyWith(
                  color: AppColors.white.withValues(alpha: 0.5),
                  fontSize: bodyFont,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: FooterLinksColumn(
            title: "Project",
            links: const ["Documentation", "GitHub"],
            titleFont: headFont,
            linkFont: linkFont,
          ),
        ),
        Expanded(
          child: FooterLinksColumn(
            title: "Company",
            links: const ["Developers", "Support"],
            titleFont: headFont,
            linkFont: linkFont,
          ),
        ),
      ],
    );
  }

  Widget _buildCompactFooter(
    double headFont,
    double bodyFont,
    double linkFont,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          appName,
          style: AppTextStyles.headlineMedium?.copyWith(
            color: AppColors.white,
            fontWeight: FontWeight.bold,
            fontSize: headFont,
          ),
        ),
        const SizedBox(height: 12),
        Text(
          "Empowering frontend developers to build visually coherent, original UI designs instantly.",
          style: AppTextStyles.titleMedium?.copyWith(
            color: AppColors.white.withValues(alpha: 0.5),
            fontSize: bodyFont,
          ),
        ),
        const SizedBox(height: 28),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: FooterLinksColumn(
                title: "Project",
                links: const ["Documentation", "GitHub"],
                titleFont: headFont,
                linkFont: linkFont,
              ),
            ),
            const SizedBox(width: 24),
            Expanded(
              child: FooterLinksColumn(
                title: "Company",
                links: const ["Developers", "Support"],
                titleFont: headFont,
                linkFont: linkFont,
              ),
            ),
          ],
        ),
      ],
    );
  }
}
