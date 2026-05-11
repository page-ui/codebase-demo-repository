part of 'pick_file_cubit.dart';

@immutable
sealed class PickFileState {}

final class PickFileInitial extends PickFileState {}

final class PickFileFailure extends PickFileState {
  final String message;
  PickFileFailure({required this.message});
}

class PickFileSuccess extends PickFileState {
  final FilePickerResult image;

  PickFileSuccess(this.image);
}
