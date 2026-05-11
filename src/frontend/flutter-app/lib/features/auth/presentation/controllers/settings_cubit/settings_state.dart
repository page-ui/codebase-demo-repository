part of 'settings_cubit.dart';

sealed class SettingsState {}

final class SettingsInitial extends SettingsState {}

final class SettingsLoading extends SettingsState {}

final class SettingsSuccess extends SettingsState {}

final class SettingsError extends SettingsState {
  final String message;

  SettingsError(this.message);
}
