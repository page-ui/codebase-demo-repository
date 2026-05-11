import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/features/auth/domain/repos/auth_repo.dart';

part 'delete_account_state.dart';

class DeleteAccountCubit extends Cubit<DeleteAccountState> {
  final AuthRepo authRepo;

  DeleteAccountCubit(this.authRepo) : super(DeleteAccountInitial());

  Future<void> requestAccountDeletion() async {
    emit(DeleteAccountRequestLoading());
    final result = await authRepo.requestAccountDeletion();
    result.fold(
      (failure) =>
          emit(DeleteAccountRequestError(message: failure.message)),
      (isSuccess) {
        if (isSuccess) {
          emit(DeleteAccountRequestSuccess());
        } else {
          emit(DeleteAccountRequestError(
              message:
                  "you can't delete your account now please try again after minutes"));
        }
      },
    );
  }

  Future<void> verifyDeletion(String code) async {
    emit(DeleteAccountVerifyLoading());
    final result = await authRepo.deleteAccount(code: code);
    result.fold(
      (failure) {
        String msg = failure.message;
        if (msg.contains('Unexpected Execution Error')) {
          msg = 'Invalid code or unexpected error occurred. Please try again.';
        }
        emit(DeleteAccountVerifyError(message: msg));
      },
      (isSuccess) {
        if (isSuccess) {
          emit(DeleteAccountVerifySuccess());
        } else {
          emit(DeleteAccountVerifyError(
              message: "Invalid code or failed to delete account."));
        }
      },
    );
  }
}
