part of 'forget_password_cubit.dart';

@immutable
sealed class ForgetPasswordState {}

final class ForgetPasswordInitial extends ForgetPasswordState {}

final class ForgetPasswordLoading extends ForgetPasswordState {}

final class ForgetPasswordFailure extends ForgetPasswordState {
  final String message;
  ForgetPasswordFailure({required this.message});
}

final class ForgetPasswordVerficationCodeSuccess extends ForgetPasswordState {
  final String code;
  ForgetPasswordVerficationCodeSuccess({required this.code});
}

final class ForgetPasswordSuccess extends ForgetPasswordState {}
