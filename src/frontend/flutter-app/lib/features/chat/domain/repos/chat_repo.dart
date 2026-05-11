import 'package:page_ui/core/errors/failure.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';
import 'package:page_ui/features/chat/domain/params/create_chat_params.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';
import 'package:dartz/dartz.dart';

abstract class ChatRepo {
  Future<Either<Failure, ChatEntity>> createChat({
    required CreateChatParams params,
  });

  Future<
    Either<
      Failure,
      ({List<ChatEntity> chats, bool hasNextPage, String? endCursor})
    >
  >
  getChats({required int first, String? after});

  Future<Either<Failure, List<ChatEntity>>> searchChats({
    required String name,
    required int first,
  });

  Future<
    Either<
      Failure,
      ({List<MessageEntity> messages, bool hasNextPage, String? endCursor})
    >
  >
  getMessages({required String chatId, required int first, String? after});

  Stream<MessageEntity> subscribeToMessages({required String chatId});

  Future<Either<Failure, void>> sendMessage({
    required SendMessageParams params,
  });

  Future<Either<Failure, void>> deleteChat({required String chatId});

  Future<Either<Failure, ChatEntity>> renameChat({
    required String chatId,
    required String name,
  });
}
