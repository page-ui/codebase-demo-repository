import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';
import 'package:page_ui/features/chat/domain/repos/chat_repo.dart';
import 'package:page_ui/features/chat/domain/usecases/upload_attachment_usecase.dart';
import 'package:dartz/dartz.dart';

class SendMessageUseCase {
  final ChatRepo _chatRepo;
  final UploadAttachmentUseCase _uploadAttachment;

  SendMessageUseCase({
    required ChatRepo chatRepo,
    required UploadAttachmentUseCase uploadAttachment,
  }) : _chatRepo = chatRepo,
       _uploadAttachment = uploadAttachment;

  Future<Either<Failure, void>> call({
    required SendMessageParams params,
    UploadAttachmentInput? attachment,
  }) async {
    SendMessageParams finalParams = params;
    if (attachment != null) {
      final downloadUrl = await _uploadAttachment(attachment);
      finalParams = params.copyWith(attachmentUrl: downloadUrl);
    }
    return _chatRepo.sendMessage(params: finalParams);
  }
}
