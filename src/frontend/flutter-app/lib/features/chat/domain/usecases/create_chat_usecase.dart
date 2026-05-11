import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/domain/params/create_chat_params.dart';
import 'package:page_ui/features/chat/domain/repos/chat_repo.dart';
import 'package:page_ui/features/chat/domain/usecases/upload_attachment_usecase.dart';
import 'package:dartz/dartz.dart';

class CreateChatUseCase {
  final ChatRepo _chatRepo;
  final UploadAttachmentUseCase _uploadAttachment;

  CreateChatUseCase({
    required ChatRepo chatRepo,
    required UploadAttachmentUseCase uploadAttachment,
  }) : _chatRepo = chatRepo,
       _uploadAttachment = uploadAttachment;

  Future<Either<Failure, ChatEntity>> call({
    required String content,
    UploadAttachmentInput? attachment,
  }) async {
    String? attachmentUrl;
    if (attachment != null) {
      attachmentUrl = await _uploadAttachment(attachment);
    }
    return _chatRepo.createChat(
      params: CreateChatParams(
        content: content,
        attachmentUrl: attachmentUrl,
      ),
    );
  }
}
