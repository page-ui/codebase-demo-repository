import 'package:flutter/material.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/core/helpers/custom_modal_progress_hud.dart';
import 'package:page_ui/features/auth/presentation/widgets/custom_auth_screen_theme.dart';
import 'package:page_ui/features/auth/presentation/widgets/login_view_body.dart';

class LoginView extends StatefulWidget {
  const LoginView({super.key});
  static const String routeName = "LoginView";

  @override
  State<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends State<LoginView> {
  bool isLoading = false;

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) {
        AppRoutes.goLanding(context);
      },
      child: Scaffold(
        resizeToAvoidBottomInset: false,

        body: 
        
        CustomModalProgressHud(
          isLoading: isLoading,
          child: CustomAuthScreenTheme(
            viewTitle: 'Login',
            child: 
            
            LoginViewBody(
              onChangeLoadingValue: (bool p1) {
                setState(() {
                  isLoading = p1;
                });
              },
            ),
          ),
        ),
      ),
    );
  }
}
