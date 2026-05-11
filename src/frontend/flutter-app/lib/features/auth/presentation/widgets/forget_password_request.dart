import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/custom_widget/custom_button.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/auth/presentation/controllers/forget_password_cubit/forget_password_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/auth_text_form_field.dart';
import 'package:page_ui/features/auth/presentation/widgets/custom_row_auth.dart';
import 'package:page_ui/features/auth/presentation/widgets/email_validator.dart';
import 'package:page_ui/features/auth/presentation/widgets/have_an_account_widget.dart';

class ForgetPasswordRequest extends StatefulWidget {
  const ForgetPasswordRequest({
    super.key,
    required this.nextStep,
    required this.onEmailChanged,
  });
  final void Function() nextStep;
  final ValueChanged<String> onEmailChanged;
  @override
  State<ForgetPasswordRequest> createState() => _ForgetPasswordRequestState();
}

class _ForgetPasswordRequestState extends State<ForgetPasswordRequest> {
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;
  GlobalKey<FormState> formKeyEmailCheck = GlobalKey<FormState>();
  TextEditingController _emailController = TextEditingController();
  bool isLoading = false;

  @override
  void dispose() {
    _emailController.dispose();
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
          showSnackBar(context: context, message: 'Check Your Email.');
          widget.nextStep();
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
        autovalidateMode: autovalidateMode,
        key: formKeyEmailCheck,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const customRowAuth(hint: "Type Your Email"),
            const SizedBox(height: 4),
            AuthTextFormField(
              controller: _emailController,
              validator: EmailValidator,
              enable: !isLoading,
            ),
            const SizedBox(height: 12),
            const HaveAnAccountWidget(),
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
                title: 'Send Code',
                onPressed: () {
                  if (formKeyEmailCheck.currentState!.validate()) {
                    final email = _emailController.text;
                    widget.onEmailChanged(email);
                    context.read<ForgetPasswordCubit>().forgotPasswordRequest(
                      email: email,
                    );
                    FocusScope.of(context).unfocus();
                    formKeyEmailCheck.currentState!.reset();
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
