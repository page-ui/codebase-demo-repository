import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/domain/params/register_params.dart';
import 'package:page_ui/features/auth/presentation/controllers/register_cubit/register_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/register_form.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class RegisterViewBody extends StatelessWidget {
  const RegisterViewBody({super.key, required this.onChangeLoadingValue});
  final void Function(bool)? onChangeLoadingValue;

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) =>
          RegisterCubit(authRepoImpl: getit.get<AuthRepoImpl>()),
      child: Builder(
        builder: (context) {
          return RegisterForm(
            onChangeLoadingValue: onChangeLoadingValue,
            onRegister: (RegisterParams params) {
              context.read<RegisterCubit>().register(
                params: RegisterParams(
                  email: params.email,
                  password: params.password,
                  userName: params.userName,
                ),
              );
            },
          );
        },
      ),
    );
  }
}
