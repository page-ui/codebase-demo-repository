import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/constants.dart';
import 'package:page_ui/core/custom_widget/animated_starfield_background.dart';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

class PageDotUi extends StatelessWidget {
  const PageDotUi({super.key});
  @override
  Widget build(BuildContext context) {
    return AnimatedStarfieldBackground(
      child: ScreenUtilInit(
        designSize: const Size(1920, 1080),
        minTextAdapt: true,
        splitScreenMode: true,
        child: MaterialApp.router(
          routerConfig: AppRoutes.router,
          title: 'Page.ui',
          debugShowCheckedModeBanner: false,
          theme: ThemeData(
            scaffoldBackgroundColor: AppColors.transparent,
            fontFamily: fontName,
          ),
          builder: (context, child) {
            AppTextStyles.init(context);
            return child ?? const SizedBox();
          },
        ),
      ),
    );
  }
}
