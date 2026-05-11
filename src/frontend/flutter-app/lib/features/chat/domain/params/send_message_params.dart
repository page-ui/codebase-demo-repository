import 'message_content_codec.dart';

class SendMessageParams {
  final String chatId;
  final String content;
  final String? attachmentUrl;

  const SendMessageParams({
    required this.chatId,
    required this.content,
    this.attachmentUrl,
  });

  SendMessageParams copyWith({
    String? chatId,
    String? content,
    String? attachmentUrl,
  }) {
    return SendMessageParams(
      chatId: chatId ?? this.chatId,
      content: content ?? this.content,
      attachmentUrl: attachmentUrl ?? this.attachmentUrl,
    );
  }

  Map<String, dynamic> toInputJson() {
    final normalizedContent = _normalizeContent(
      content: content,
      attachmentUrl: attachmentUrl,
    );

    return {
      'chatKey': chatId,
      'content': normalizedContent,
      if (attachmentUrl != null && attachmentUrl!.trim().isNotEmpty)
        'attachmentUrl': attachmentUrl,
    };
  }
}

String _normalizeContent({
  required String content,
  required String? attachmentUrl,
}) {
  final normalizedLineEndings = encodeMessageContent(content);

  if (attachmentUrl == null || attachmentUrl.trim().isEmpty) {
    return normalizedLineEndings;
  }

  return normalizedLineEndings;
}
