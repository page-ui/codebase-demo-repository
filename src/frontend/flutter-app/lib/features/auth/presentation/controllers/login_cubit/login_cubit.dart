import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/domain/params/login_params.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:meta/meta.dart';

part 'login_state.dart';

class LoginCubit extends Cubit<LoginState> {
  LoginCubit({required this.authRepoImpl}) : super(LoginInitial());
  final AuthRepoImpl authRepoImpl;
  Future<void> login({required LoginParams params}) async {
    emit(LoginLoading());
    final result = await authRepoImpl.login(param: params);
    await result.fold(
      (failure) {
        emit(LoginFailure(message: failure.message));
      },
      (user) async {
        emit(LoginSuccess());
      },
    );
  }
}
