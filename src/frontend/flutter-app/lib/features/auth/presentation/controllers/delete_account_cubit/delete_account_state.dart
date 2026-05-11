part of 'delete_account_cubit.dart';

abstract class DeleteAccountState {}

class DeleteAccountInitial extends DeleteAccountState {}

class DeleteAccountRequestLoading extends DeleteAccountState {}

class DeleteAccountRequestSuccess extends DeleteAccountState {}

class DeleteAccountRequestError extends DeleteAccountState {
  final String message;
  DeleteAccountRequestError({required this.message});
}

class DeleteAccountVerifyLoading extends DeleteAccountState {}

class DeleteAccountVerifySuccess extends DeleteAccountState {}

class DeleteAccountVerifyError extends DeleteAccountState {
  final String message;
  DeleteAccountVerifyError({required this.message});
}
