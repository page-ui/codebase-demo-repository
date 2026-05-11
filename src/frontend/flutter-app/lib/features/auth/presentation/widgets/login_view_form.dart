import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/core/custom_widget/custom_button.dart';
import 'package:page_ui/core/helpers/auth_state.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/auth/presentation/controllers/login_cubit/login_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/auth_text_form_field.dart';
import 'package:page_ui/features/auth/presentation/widgets/custom_row_auth.dart';
import 'package:page_ui/features/auth/presentation/widgets/do_not_have_an_account_widget.dart';
import 'package:page_ui/features/auth/presentation/widgets/email_validator.dart';
import 'package:page_ui/features/auth/presentation/widgets/forget_password_widget.dart';
import 'package:page_ui/features/auth/presentation/widgets/password_text_form_field.dart';

class LoginViewForm extends StatefulWidget {
  LoginViewForm({
    super.key,
    required this.onChangeLoadingValue,
    required this.onLogin,
  });
  final void Function(bool)? onChangeLoadingValue;
  final void Function(String email, String password) onLogin;

  @override
  State<LoginViewForm> createState() => _LoginViewFormState();
}

class _LoginViewFormState extends State<LoginViewForm> {
  final GlobalKey<FormState> formKey = GlobalKey<FormState>();
  late final TextEditingController _emailController;
  late final TextEditingController _passwordController;
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;

  @override
  void initState() {
    super.initState();
    _emailController = TextEditingController();
    _passwordController = TextEditingController();
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocListener<LoginCubit, LoginState>(
      listener: (context, state) {
        if (state is LoginSuccess) {
          showSnackBar(context: context, message: "Login Success");
          widget.onChangeLoadingValue!(false);
          AuthState.setLoggedIn(true);
          AppRoutes.goTrain(context);
        } else if (state is LoginFailure) {
          showSnackBar(context: context, message: state.message);
          widget.onChangeLoadingValue!(false);
        } else if (state is LoginLoading) {
          widget.onChangeLoadingValue!(true);
        } else {
          widget.onChangeLoadingValue!(false);
        }
      },
      child: Form(
        key: formKey,
        autovalidateMode: autovalidateMode,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const customRowAuth(hint: "Email"),
            const SizedBox(height: 4),
            AuthTextFormField(
              controller: _emailController,
              validator: EmailValidator,
            ),
            const SizedBox(height: 16),
            const customRowAuth(hint: "Password"),
            const SizedBox(height: 4),
            PasswordTextFormField(controller: _passwordController),
            const SizedBox(height: 12),
            const DoNotHaveAnAccountWidget(),
            const SizedBox(height: 12),
            const ForgetPasswordWidget(),
            const SizedBox(height: 20),
            Center(
              child: CustomButton(
                title: 'Login',
                onPressed: () {
                  if (formKey.currentState!.validate()) {
                    widget.onLogin(
                      _emailController.text,
                      _passwordController.text,
                    );
                    FocusScope.of(context).unfocus();
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
