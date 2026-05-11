import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/core/helpers/app_logger.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/domain/usecases/create_chat_usecase.dart';
import 'package:page_ui/features/chat/domain/usecases/upload_attachment_usecase.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/pick_file_cubit/pick_file_cubit.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class ChatHomeCubit extends Cubit<ChatHomeState> {
  final CreateChatUseCase _createChat;

  ChatHomeCubit({required CreateChatUseCase createChat})
    : _createChat = createChat,
      super(const ChatHomeInitial());

  Future<void> createChat({
    required String content,
    UploadAttachmentInput? attachment,
  }) async {
    if (state is ChatHomeLoading) return;
    final currentChat = state.selectedChat;
    emit(ChatHomeLoading(previousChat: currentChat));

    try {
      final result = await _createChat(
        content: content,
        attachment: attachment,
      );
      if (isClosed) return;
      result.fold(
        (failure) => emit(
          ChatHomeError(message: failure.message, previousChat: currentChat),
        ),
        (chat) => emit(ChatHomeActive(chat: chat, isNewlyCreated: true)),
      );
    } catch (e, stackTrace) {
      appLogger.e(
        'ChatHomeCubit.createChat unexpected',
        error: e,
        stackTrace: stackTrace,
      );
      if (isClosed) return;
      emit(
        ChatHomeError(
          message: ServerFailure.forOperation(AppOperation.createChat).message,
          previousChat: currentChat,
        ),
      );
    }
  }

  Future<void> createChatWithPicker({
    required String content,
    required PickFileCubit pickFileCubit,
  }) async {
    final attachment = pickFileCubit.isImagePicked
        ? UploadAttachmentInput(
            bytes: pickFileCubit.imageBytes!,
            fileName: pickFileCubit.imageFileName!,
            contentType: pickFileCubit.imageContentType!,
          )
        : null;

    await createChat(
      content: content,
      attachment: attachment,
    );

    if (pickFileCubit.isImagePicked) {
      pickFileCubit.removeImage();
    }
  }

  void selectChat({required ChatEntity chat}) {
    emit(ChatHomeActive(chat: chat));
  }

  void updateSelectedChat({required ChatEntity chat}) {
    if (state.selectedChat?.id != chat.id) return;
    emit(ChatHomeActive(chat: chat));
  }

  void reset() {
    emit(const ChatHomeInitial());
  }
}
