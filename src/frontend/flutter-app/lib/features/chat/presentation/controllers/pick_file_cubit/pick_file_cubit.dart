import 'dart:typed_data';

import 'package:bloc/bloc.dart';
import 'package:file_picker/file_picker.dart';
import 'package:meta/meta.dart';

part 'pick_file_state.dart';

class PickFileCubit extends Cubit<PickFileState> {
  PickFileCubit() : super(PickFileInitial());

  static const int _maxFileBytes = 5 * 1024 * 1024;

  static const _mimeTypes = <String, String>{
    'jpg': 'image/jpeg',
    'jpeg': 'image/jpeg',
    'png': 'image/png',
    'webp': 'image/webp',
  };

  FilePickerResult? _image;

  bool pickImage({required FilePickerResult? imageFile}) {
    if (imageFile == null || imageFile.files.isEmpty) {
      emit(PickFileFailure(message: 'No file was selected. Please try again.'));
      return false;
    }

    final file = imageFile.files.first;
    final ext = file.extension?.toLowerCase();

    if (ext == null || !_mimeTypes.containsKey(ext)) {
      emit(PickFileFailure(message: 'Only image files are allowed.'));
      return false;
    }

    if (file.size > _maxFileBytes) {
      emit(PickFileFailure(message: 'The file is larger than 5 MB.'));
      return false;
    }

    _image = imageFile;
    emit(PickFileSuccess(imageFile));
    return true;
  }

  Uint8List? get imageBytes => _image?.files.first.bytes;

  String? get imageFileName => _image?.files.first.name;

  String? get imageContentType =>
      _mimeTypes[_image?.files.first.extension?.toLowerCase()] ?? 'image/png';

  bool get isImagePicked => _image != null;

  void removeImage() {
    _image = null;
    emit(PickFileInitial());
  }
}
