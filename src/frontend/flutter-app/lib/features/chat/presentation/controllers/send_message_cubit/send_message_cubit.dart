import 'dart:typed_data';

import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/core/helpers/app_logger.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';
import 'package:page_ui/features/chat/domain/usecases/send_message_usecase.dart';
import 'package:page_ui/features/chat/domain/usecases/upload_attachment_usecase.dart';
import 'package:page_ui/features/chat/presentation/controllers/send_message_cubit/send_message_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class SendMessageCubit extends Cubit<SendMessageState> {
  final SendMessageUseCase _sendMessage;
  Uint8List? _imageBytes;
  String? _imageFileName;
  String? _imageContentType;

  SendMessageCubit({required SendMessageUseCase sendMessage})
    : _sendMessage = sendMessage,
      super(const SendMessageInitial());

  void setImageData({
    required Uint8List bytes,
    required String fileName,
    required String contentType,
  }) {
    _imageBytes = bytes;
    _imageFileName = fileName;
    _imageContentType = contentType;
  }

  void clearImageData() {
    _imageBytes = null;
    _imageFileName = null;
    _imageContentType = null;
  }

  bool get hasImage => _imageBytes != null;

  Future<void> sendMessage({required SendMessageParams params}) async {
    emit(const SendMessageLoading());

    try {
      UploadAttachmentInput? attachment;
      if (_imageBytes != null && _imageFileName != null) {
        attachment = UploadAttachmentInput(
          bytes: _imageBytes!,
          fileName: _imageFileName!,
          contentType: _imageContentType ?? 'image/png',
        );
      }

      final result = await _sendMessage(params: params, attachment: attachment);
      if (isClosed) return;

      result.fold(
        (failure) => emit(SendMessageError(message: failure.message)),
        (_) {
          if (attachment != null) clearImageData();
          emit(const SendMessageSuccess());
        },
      );
    } catch (e, stackTrace) {
      appLogger.e(
        'SendMessageCubit.sendMessage unexpected',
        error: e,
        stackTrace: stackTrace,
      );
      if (isClosed) return;
      emit(
        SendMessageError(
          message: ServerFailure.forOperation(AppOperation.sendMessage).message,
        ),
      );
    }
  }
}
