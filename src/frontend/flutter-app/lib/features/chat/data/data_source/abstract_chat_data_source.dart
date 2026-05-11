import 'package:page_ui/features/chat/data/models/chat_model.dart';
import 'package:page_ui/features/chat/data/models/message_model.dart';
import 'package:page_ui/features/chat/domain/params/create_chat_params.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';

abstract class ChatDataSource {
  Future<ChatModel> createChat({required CreateChatParams params});

  Future<({List<ChatModel> chats, bool hasNextPage, String? endCursor})>
  getChats({required int first, String? after});

  Future<List<ChatModel>> searchChats({
    required String name,
    required int first,
  });

  Future<({List<MessageModel> messages, bool hasNextPage, String? endCursor})>
  getMessages({required String chatId, required int first, String? after});

  Stream<MessageModel> subscribeToMessages({required String chatId});

  Future<void> sendMessage({required SendMessageParams params});

  Future<void> deleteChat({required String chatId});

  Future<ChatModel> renameChat({required String chatId, required String name});
}
