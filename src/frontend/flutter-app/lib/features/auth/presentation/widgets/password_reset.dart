import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/custom_widget/custom_button.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/auth/domain/params/reset_password.dart';
import 'package:page_ui/features/auth/presentation/controllers/forget_password_cubit/forget_password_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/custom_row_auth.dart';
import 'package:page_ui/features/auth/presentation/widgets/password_text_form_field.dart';

class PasswordReset extends StatefulWidget {
  const PasswordReset({super.key, required this.email, required this.token});
  final String token;
  final String email;
  @override
  State<PasswordReset> createState() => _PasswordResetState();
}

class _PasswordResetState extends State<PasswordReset> {
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;
  GlobalKey<FormState> formKeyPasswordReset = GlobalKey<FormState>();
  TextEditingController _passwordController = TextEditingController();
  TextEditingController _confirmPasswordController = TextEditingController();
  bool isLoading = false;

  @override
  void dispose() {
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocListener<ForgetPasswordCubit, ForgetPasswordState>(
      listener: (context, state) {
        if (state is ForgetPasswordSuccess) {
          setState(() {
            isLoading = false;
          });
          showSnackBar(
            context: context,
            message: 'Password Reset Successfully.',
          );
          formKeyPasswordReset.currentState!.reset();
          AppRoutes.goLogin(context);
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
        key: formKeyPasswordReset,
        autovalidateMode: autovalidateMode,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const customRowAuth(hint: "New Password"),
            const SizedBox(height: 4),
            PasswordTextFormField(controller: _passwordController),
            const SizedBox(height: 16),
            const customRowAuth(hint: "Confirm Password"),
            const SizedBox(height: 4),
            PasswordTextFormField(controller: _confirmPasswordController),
            const SizedBox(height: 20),
            AbsorbPointer(
              absorbing: isLoading,
              child: CustomButton(
                child: isLoading
                    ? const Padding(
                        padding: EdgeInsets.all(8.0),
                        child: CircularProgressIndicator(),
                      )
                    : null,
                title: 'Reset Password',
                onPressed: () {
                  if (formKeyPasswordReset.currentState!.validate()) {
                    if (_confirmPasswordController.text ==
                        _passwordController.text) {
                      FocusScope.of(context).unfocus();
                      context.read<ForgetPasswordCubit>().resetPassword(
                        params: ResetPasswordParams(
                          email: widget.email,
                          newPassword: _passwordController.text,
                          token: widget.token,
                        ),
                      );
                    } else {
                      showSnackBar(
                        context: context,
                        message: "Password and Confirm Password must be same.",
                        backgroundColor: AppColors.red,
                        textColor: AppColors.white,
                      );
                    }
                  } else {
                    setState(() {
                      autovalidateMode = AutovalidateMode.always;
                    });
                  }
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
