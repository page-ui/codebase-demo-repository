import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/constants/constants.dart';
import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/features/auth/data/model/user_tokens_model.dart';
import 'package:animated_text_kit/animated_text_kit.dart';
import 'package:flutter/material.dart';

class SplashView extends StatefulWidget {
  const SplashView({super.key});
  static const String routeName = "SplashView";

  @override
  State<SplashView> createState() => _SplashViewState();
}

class _SplashViewState extends State<SplashView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _navigateToNextPage();
    });
  }

  void _navigateToNextPage() async {
    UserTokensModel userTokenModel = await returnTokensFromSecureDB();
    GraphQLConfig.accessToken = userTokenModel.accessToken;
    GraphQLConfig.refreshToken = userTokenModel.refreshToken;
    Future.delayed(const Duration(milliseconds: 4500), () {
      if (!mounted) return;
      userTokenModel.isEmpty()
          ? AppRoutes.goLogin(context)
          : AppRoutes.goHome(context);
    });
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        AppRoutes.goLanding(context);
      },
      child: Scaffold(
        body: Padding(
          padding: const EdgeInsets.only(top: 130.0),
          child: Center(
            child: Column(
              children: [
                DefaultTextStyle(
                  style: AppTextStyles.displayLarge!.copyWith(
                    color: AppColors.white,
                  ),
                  child: AnimatedTextKit(
                    totalRepeatCount: 1,
                    animatedTexts: [
                      WavyAnimatedText(
                        appName,
                        speed: const Duration(milliseconds: 200),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 32),
                DefaultTextStyle(
                  style: AppTextStyles.titleMedium!.copyWith(
                    color: AppColors.white,
                  ),
                  child: AnimatedTextKit(
                    totalRepeatCount: 1,
                    animatedTexts: [
                      TyperAnimatedText(
                        "initializing page.ui....",
                        speed: const Duration(milliseconds: 200),
                      ),
                    ],
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
