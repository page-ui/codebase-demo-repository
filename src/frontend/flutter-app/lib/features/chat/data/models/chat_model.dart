import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';

class ChatModel extends ChatEntity {
  const ChatModel({required super.id, required super.name, super.createdAt});

  factory ChatModel.fromJson(Map<String, dynamic> json) {
    final chatJson = (json as Map<String, dynamic>?) ?? json;

    return ChatModel(
      id: chatJson['chatKey'] as String,
      name: chatJson['name'] ?? "New Chat",
      createdAt: chatJson['createdAt'] != null
          ? DateTime.parse(chatJson['createdAt'] as String)
          : null,
    );
  }
}
