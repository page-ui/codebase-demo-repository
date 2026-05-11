import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/presentation/controllers/forget_password_cubit/forget_password_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/forget_pasword_view_form.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class ForgetPaswordViewBody extends StatelessWidget {
  const ForgetPaswordViewBody({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) =>
          ForgetPasswordCubit(authRepoImpl: getit.get<AuthRepoImpl>()),
      child: const ForgetPaswordViewForm(),
    );
  }
}
