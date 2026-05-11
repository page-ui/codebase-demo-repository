import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/custom_widget/custom_button.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/auth/domain/params/verify_reset_code_params.dart';
import 'package:page_ui/features/auth/presentation/controllers/forget_password_cubit/forget_password_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/resend_the_verfication_code_button.dart';
import 'package:page_ui/features/auth/presentation/widgets/verify_o_t_p_widget.dart';

class OTPCodeVerfication extends StatefulWidget {
  const OTPCodeVerfication({
    super.key,
    this.controllers = const [],
    this.nextStep = null,
    required this.email,
    this.onGetToken = null,
  });
  final ValueChanged<String>? onGetToken;

  final List<TextEditingController> controllers;
  final void Function()? nextStep;
  final String email;
  @override
  State<OTPCodeVerfication> createState() => _OTPCodeVerficationState();
}

class _OTPCodeVerficationState extends State<OTPCodeVerfication> {
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;
  GlobalKey<FormState> formKeyCodeVerify = GlobalKey<FormState>();
  bool isLoading = false;

  @override
  Widget build(BuildContext context) {
    var text = const Text(
      "VERIFY OTP",
      textAlign: TextAlign.center,
      style: TextStyle(
        color: AppColors.primaryColor,
        fontSize: 22,
        letterSpacing: 1.5,
        overflow: TextOverflow.clip,
      ),
    );
    return BlocListener<ForgetPasswordCubit, ForgetPasswordState>(
      listener: (context, state) {
        if (state is ForgetPasswordVerficationCodeSuccess) {
          widget.onGetToken?.call(state.code);
          setState(() {
            isLoading = false;
          });
          showSnackBar(context: context, message: 'OTP Verified.');
          widget.nextStep?.call();
        } else if (state is ForgetPasswordFailure) {
          setState(() {
            isLoading = false;
          });
          showSnackBar(
            context: context,
            message: state.message,
            backgroundColor: AppColors.red,
            textColor: AppColors.white,
          );
        } else if (state is ForgetPasswordLoading) {
          setState(() {
            isLoading = true;
          });
        }
      },
      child: Form(
        key: formKeyCodeVerify,
        autovalidateMode: autovalidateMode,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            text,
            const SizedBox(height: 8),
            const Text(
              "ENTER THE 5-DIGIT CODE SENT TO YOU",
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.primaryColor, fontSize: 13),
            ),
            const SizedBox(height: 24),

            VerifyOTPWidget(controllers: widget.controllers),
            const SizedBox(height: 30),
            ResendTheVerficationCodeButton(
              onPressed: () {
                context.read<ForgetPasswordCubit>().forgotPasswordRequest(
                  email: widget.email,
                );
              },
            ),

            const SizedBox(height: 8),
            CustomButton(
              title: "VERIFY",
              onPressed: () {
                for (var controller in widget.controllers) {
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
                  context.read<ForgetPasswordCubit>().verifyResetCode(
                    params: VerifyResetCodeParams(
                      email: widget.email,
                      code: widget.controllers.map((e) => e.text).join(),
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
              alignment: Alignment.bottomLeft,
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
