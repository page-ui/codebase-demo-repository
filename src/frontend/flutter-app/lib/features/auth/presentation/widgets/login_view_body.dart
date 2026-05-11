import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:page_ui/features/auth/presentation/controllers/login_cubit/login_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/login_view_form.dart';

class LoginViewBody extends StatelessWidget {
  const LoginViewBody({super.key, required this.onChangeLoadingValue});
  final void Function(bool)? onChangeLoadingValue;

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => LoginCubit(authRepoImpl: getit.get<AuthRepoImpl>()),
      child: Builder(
        builder: (context) {
          return LoginViewForm(
            onChangeLoadingValue: onChangeLoadingValue,
            onLogin: (String email, String password) {
              context.read<LoginCubit>().login(
                params: LoginParams(email: email, password: password),
              );
            },
          );
        },
      ),
    );
  }
}
