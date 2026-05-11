import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/domain/params/reset_password.dart';
import 'package:page_ui/features/auth/domain/params/verify_reset_code_params.dart';
import 'package:bloc/bloc.dart';
import 'package:meta/meta.dart';

part 'forget_password_state.dart';

class ForgetPasswordCubit extends Cubit<ForgetPasswordState> {
  ForgetPasswordCubit({required this.authRepoImpl})
    : super(ForgetPasswordInitial());
  final AuthRepoImpl authRepoImpl;
  Future<void> forgotPasswordRequest({required String email}) async {
    emit(ForgetPasswordLoading());
    final result = await authRepoImpl.forgotPasswordRequest(email: email);
    result.fold(
      (failure) {
        emit(ForgetPasswordFailure(message: failure.message));
      },
      (res) {
        if (res) {
          emit(ForgetPasswordSuccess());
        } else {
          emit(
            ForgetPasswordFailure(message: "May be you write a wrong account."),
          );
        }
      },
    );
  }

  Future<String> verifyResetCode({
    required VerifyResetCodeParams params,
  }) async {
    emit(ForgetPasswordLoading());
    final result = await authRepoImpl.verifyResetCode(params: params);
    result.fold(
      (failure) {
        emit(ForgetPasswordFailure(message: failure.message));
        return "";
      },
      (res) {
        if (res.isEmpty) {
          emit(
            ForgetPasswordFailure(
              message:
                  "There was a propblem, please make sure you're write the code correct, or resend the code.",
            ),
          );
          return "";
        }
        emit(ForgetPasswordVerficationCodeSuccess(code: res));
        return res;
      },
    );
    return "";
  }

  
  Future<void> resetPassword({required ResetPasswordParams params}) async {
    emit(ForgetPasswordLoading());
    final result = await authRepoImpl.changePassword(
      params: params,
    ); 
    result.fold(
      (failure) => emit(ForgetPasswordFailure(message: failure.message)),
      (_) => emit(ForgetPasswordSuccess()),
    );
  }
}
