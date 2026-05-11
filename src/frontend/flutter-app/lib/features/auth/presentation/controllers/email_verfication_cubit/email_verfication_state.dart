part of 'email_verfication_cubit.dart';

@immutable
sealed class EmailVerficationState {}

final class EmailVerficationInitial extends EmailVerficationState {}

final class EmailVerificationFailure extends EmailVerficationState {
  final String message;

  EmailVerificationFailure({required this.message});
}

final class EmailVerificationLoading extends EmailVerficationState {}

final class EmailVerificationnSuccess extends EmailVerficationState {}

final class ResendTheCodeSuccess extends EmailVerficationState {}
