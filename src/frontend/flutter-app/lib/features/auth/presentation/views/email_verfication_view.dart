import 'package:page_ui/core/helpers/custom_modal_progress_hud.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:page_ui/features/auth/presentation/widgets/custom_auth_screen_theme.dart';
import 'package:page_ui/features/auth/presentation/widgets/email_verfication_view_body.dart';
import 'package:flutter/material.dart';

class EmailVerficationView extends StatefulWidget {
  const EmailVerficationView({super.key, required this.param});
  final LoginParams param;
  static const String routeName = "EmailVerficationView";

  @override
  State<EmailVerficationView> createState() => _EmailVerficationViewState();
}

class _EmailVerficationViewState extends State<EmailVerficationView> {
  bool isLoading = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: CustomModalProgressHud(
        isLoading: isLoading,

        child: CustomAuthScreenTheme(
          viewTitle: 'Email Verfication',
          child: EmailVerficationViewBody(
            widget: widget,
            onChangeLoadingValue: (bool p1) {
              setState(() {
                isLoading = p1;
              });
            },
          ),
        ),
      ),
    );
  }
}
