import 'dart:typed_data';

import 'package:page_ui/features/chat/data/data_source/upload_service.dart';

class UploadAttachmentInput {
  final Uint8List bytes;
  final String fileName;
  final String contentType;

  const UploadAttachmentInput({
    required this.bytes,
    required this.fileName,
    required this.contentType,
  });
}

class UploadAttachmentUseCase {
  final UploadService _uploadService;

  UploadAttachmentUseCase({required UploadService uploadService})
    : _uploadService = uploadService;

  Future<String> call(UploadAttachmentInput input) {
    return _uploadService.upload(
      fileBytes: input.bytes,
      fileName: input.fileName,
      contentType: input.contentType,
    );
  }
}
