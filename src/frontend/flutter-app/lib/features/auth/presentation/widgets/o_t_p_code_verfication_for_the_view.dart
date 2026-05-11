import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/core/helpers/auth_state.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/custom_widget/custom_button.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:page_ui/features/auth/domain/params/verify_reset_code_params.dart';
import 'package:page_ui/features/auth/presentation/controllers/email_verfication_cubit/email_verfication_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/resend_the_verfication_code_button.dart';
import 'package:page_ui/features/auth/presentation/widgets/verify_o_t_p_widget.dart';

class EmailVerficationForm extends StatefulWidget {
  const EmailVerficationForm({
    super.key,
    required this.param,
    required this.onChangeLoadingValue,
    required this.onPressed,
  });
  final void Function(bool)? onChangeLoadingValue;
  final void Function(VerifyResetCodeParams) onPressed;
  final LoginParams param;
  @override
  State<EmailVerficationForm> createState() => _EmailVerficationFormState();
}

class _EmailVerficationFormState extends State<EmailVerficationForm> {
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;
  GlobalKey<FormState> formKeyCodeVerify = GlobalKey<FormState>();
  bool isLoading = false;
  List<TextEditingController> controllers = [];

  void initState() {
    super.initState();
    controllers = List.generate(5, (_) => TextEditingController());
  }

  @override
  void dispose() {
    controllers.forEach((controller) => controller.dispose());
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    var text = const Text(
      "VERIFY OTP",
      style: TextStyle(
        color: AppColors.primaryColor,
        fontSize: 22,
        letterSpacing: 1.5,
        overflow: TextOverflow.clip,
      ),
    );
    return BlocListener<EmailVerificationCubit, EmailVerficationState>(
      listener: (context, state) {
        if (state is EmailVerificationnSuccess) {
          setState(() {
            isLoading = false;
            widget.onChangeLoadingValue!(isLoading);
          });
          showSnackBar(context: context, message: 'OTP Verified.');
          AuthState.setLoggedIn(true);
          AppRoutes.goTrain(context);
        } else if (state is EmailVerificationFailure) {
          setState(() {
            isLoading = false;
            widget.onChangeLoadingValue!(isLoading);
          });
          showSnackBar(
            context: context,
            message: state.message,
            backgroundColor: AppColors.red,
            textColor: AppColors.white,
          );
        } else if (state is EmailVerificationLoading) {
          setState(() {
            isLoading = true;
            widget.onChangeLoadingValue!(isLoading);
          });
        } else if (state is ResendTheCodeSuccess) {
          showSnackBar(context: context, message: 'Check Your Email.');
          setState(() {
            isLoading = false;
            widget.onChangeLoadingValue!(isLoading);
          });
        }
      },
      child: Form(
        key: formKeyCodeVerify,
        autovalidateMode: autovalidateMode,
        child: Column(
          children: [
            text,
            const SizedBox(height: 8),
            const Text(
              "ENTER THE 5-DIGIT CODE SENT TO YOU",
              style: TextStyle(color: AppColors.primaryColor, fontSize: 13),
            ),
            const SizedBox(height: 24),

            VerifyOTPWidget(controllers: controllers),
            const SizedBox(height: 30),
            ResendTheVerficationCodeButton(
              onPressed: () {
                context.read<EmailVerificationCubit>().resendTheVerficationCode(
                  email: widget.param.email,
                );
              },
            ),

            const SizedBox(height: 8),
            CustomButton(
              title: "VERIFY",
              onPressed: () {
                for (var controller in controllers) {
                  if (controller.text.isEmpty) {
                    showSnackBar(
                      context: context,
                      message: 'OTP not completed.',
                      backgroundColor: AppColors.red,
                      textColor: AppColors.white,
                    );
                    return;
                  }
                }
                if (formKeyCodeVerify.currentState!.validate()) {
                  FocusScope.of(context).unfocus();
                  widget.onPressed(
                    VerifyResetCodeParams(
                      email: widget.param.email,
                      code: controllers.map((e) => e.text).join(),
                    ),
                  );
                  formKeyCodeVerify.currentState!.reset();
                } else {
                  setState(() {
                    autovalidateMode = AutovalidateMode.always;
                  });
                }
              },
            ),
            const SizedBox(height: 8),
            Align(
              alignment: AlignmentGeometry.bottomLeft,
              child: Text(
                "Note: email maybe in spam emails.",
                style: AppTextStyles.bodyMedium!.copyWith(color: AppColors.red),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
