import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/features/auth/domain/repos/auth_repo.dart';

part 'settings_state.dart';

class SettingsCubit extends Cubit<SettingsState> {
  final AuthRepo _authRepo;

  SettingsCubit(this._authRepo) : super(SettingsInitial());

  Future<void> signOut() async {
    emit(SettingsLoading());
    final result = await _authRepo.signOut();
    if (isClosed) return;

    result.fold(
      (failure) => emit(SettingsError(failure.message)),
      (_) => emit(SettingsSuccess()),
    );
  }
}
