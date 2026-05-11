class MessageEntity {
  final String id;
  final String chatId;
  final String? senderId;
  final String content;
  final String type;
  final String status;
  final DateTime createdAt;
  final String? attachmentUrl;
  final bool isDeleted;
  final bool isQuestion;

  const MessageEntity({
    required this.id,
    required this.chatId,
    this.senderId,
    required this.content,
    required this.type,
    required this.status,
    required this.createdAt,
    this.attachmentUrl,
    this.isDeleted = false,
    this.isQuestion = false,
  });
}
