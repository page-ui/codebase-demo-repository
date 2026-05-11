import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/core/custom_widget/custom_divider.dart';
import 'package:page_ui/core/custom_widget/dots.dart';
import 'package:page_ui/core/custom_widget/logo_widget.dart';
import 'package:page_ui/core/custom_widget/scramb_animated_title_text.dart';
import 'package:page_ui/core/enum/screen_type.dart';

class CustomAuthScreenTheme extends StatelessWidget {
  const CustomAuthScreenTheme({
    super.key,
    required this.child,
    required this.viewTitle,
  });

  final Widget child;
  final String viewTitle;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        double maxWidth;
        bool isMobile = false;
        if (context.isDesktop) {
          maxWidth = 500;
        } else if (context.isTablet) {
          maxWidth = 500;
        } else if (context.isMobile) {
          maxWidth = double.infinity;
          isMobile = true;
        } else {
          maxWidth = constraints.maxWidth * 0.62;
        }
        return Center(
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Visibility(visible: !isMobile, child: const LogoWidget()),
                Visibility(
                  visible: !isMobile,
                  child: const SizedBox(height: 24),
                ),
                ConstrainedBox(
                  constraints: BoxConstraints(
                    maxWidth: maxWidth,
                    minHeight: isMobile ? MediaQuery.sizeOf(context).height : 0,
                  ),
                  child: Container(
                    decoration: BoxDecoration(
                      boxShadow: [
                        BoxShadow(
                          color: AppColors.green.withValues(alpha: 0.6),
                          blurRadius: 26,
                          spreadRadius: 2,
                          blurStyle: BlurStyle.outer,
                        ),
                      ],
                      borderRadius: isMobile
                          ? AppBorders.zero
                          : AppBorders.xxxxs,
                      border: Border.all(
                        color: AppColors.primaryColor,
                        width: 2,
                        strokeAlign: BorderSide.strokeAlignOutside,
                      ),
                      color: isMobile
                          ? AppColors.transparent
                          : AppColors.mainBackgroundColor,
                    ),
                    child: Padding(
                      padding: EdgeInsets.symmetric(
                        vertical: isMobile ? 50 : 20,
                        horizontal: 24,
                      ),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Visibility(visible: !isMobile, child: Dots()),
                          Visibility(
                            visible: isMobile,
                            child: const LogoWidget(),
                          ),
                          const CustomDivider(),
                          const SizedBox(height: 8),
                          ScrambAnimatedTitleText(viewTitle: viewTitle),
                          const SizedBox(height: 8),
                          const CustomDivider(),
                          const SizedBox(height: 16),
                          child,
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
