import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/domain/params/register_params.dart';
import 'package:bloc/bloc.dart';
import 'package:meta/meta.dart';

part 'register_state.dart';

class RegisterCubit extends Cubit<RegisterState> {
  RegisterCubit({required this.authRepoImpl}) : super(RegisterInitial());
  final AuthRepoImpl authRepoImpl;
  Future<void> register({required RegisterParams params}) async {
    emit(RegisterLoading());
    final result = await authRepoImpl.register(param: params);

    await result.fold(
      (failure) {
        emit(RegisterFailure(message: failure.message));
      },
      (user) async {
        emit(RegisterSuccess());
      },
    );
  }
}
