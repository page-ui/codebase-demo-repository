import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/error_model.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';
import 'package:page_ui/features/chat/domain/params/message_content_codec.dart';

class MessageModel extends MessageEntity {
  static const String defaultStatus = 'sent';

  const MessageModel({
    required super.id,
    required super.chatId,
    super.senderId,
    required super.content,
    required super.type,
    required super.status,
    required super.createdAt,
    super.attachmentUrl,
    super.isDeleted,
    super.isQuestion,
  });

  factory MessageModel.fromJson(
    Map<String, dynamic> json, {
    String? fallbackChatId,
  }) {
    final chatId = (json['chatKey'] as String?) ?? (json['chatId'] as String?) ?? fallbackChatId;
    if (chatId == null || chatId.trim().isEmpty) {
      throw BadResponseException(
        ErrorModel(status: 0, errorMessage: AppOperation.parseMessage.name),
        operation: AppOperation.parseMessage,
      );
    }

    return MessageModel(
      id: (json['messageKey'] ?? json['id']) as String,
      chatId: chatId,
      senderId: (json['senderType'] ?? json['senderId']) as String?,
      content: decodeMessageContent((json['content'] ?? '') as String),
      type: json['type'] as String,
      status: (json['status'] as String?) ?? defaultStatus,
      createdAt: DateTime.parse(json['createdAt'] as String),
      attachmentUrl: json['attachmentUrl'] as String?,
      isDeleted: (json['isDeleted'] as bool?) ?? false,
      isQuestion: (json['isQuestion'] as bool?) ?? false,
    );
  }
}
