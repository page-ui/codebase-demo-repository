import 'package:page_ui/features/chat/domain/constants/message_types.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';

sealed class ChatMessagesState {
  const ChatMessagesState();
}

final class ChatMessagesInitial extends ChatMessagesState {
  const ChatMessagesInitial();
}

final class ChatMessagesLoading extends ChatMessagesState {
  final String chatId;

  const ChatMessagesLoading({required this.chatId});
}

final class ChatMessagesLoaded extends ChatMessagesState {
  final String chatId;
  final List<MessageEntity> messages;
  final bool hasNextPage;
  final String? endCursor;
  final bool isLoadingMore;
  final String? selectedAiRunMessageId;
  final bool isAwaitingAiResponse;
  final MessageEntity? activeThinkingMessage;
  final bool isSubscriptionActive;

  const ChatMessagesLoaded({
    required this.chatId,
    required this.messages,
    this.hasNextPage = false,
    this.endCursor,
    this.isLoadingMore = false,
    this.selectedAiRunMessageId,
    this.isAwaitingAiResponse = false,
    this.activeThinkingMessage,
    this.isSubscriptionActive = false,
  });

  ChatMessagesLoaded copyWith({
    String? chatId,
    List<MessageEntity>? messages,
    bool? hasNextPage,
    String? endCursor,
    bool? isLoadingMore,
    String? selectedAiRunMessageId,
    bool? isAwaitingAiResponse,
    MessageEntity? activeThinkingMessage,
    bool? isSubscriptionActive,
  }) {
    return ChatMessagesLoaded(
      chatId: chatId ?? this.chatId,
      messages: messages ?? this.messages,
      hasNextPage: hasNextPage ?? this.hasNextPage,
      endCursor: endCursor ?? this.endCursor,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      selectedAiRunMessageId:
          selectedAiRunMessageId ?? this.selectedAiRunMessageId,
      isAwaitingAiResponse: isAwaitingAiResponse ?? this.isAwaitingAiResponse,
      activeThinkingMessage:
          activeThinkingMessage ?? this.activeThinkingMessage,
      isSubscriptionActive: isSubscriptionActive ?? this.isSubscriptionActive,
    );
  }
}

final class ChatMessagesError extends ChatMessagesState {
  final String chatId;
  final String message;

  const ChatMessagesError({required this.chatId, required this.message});
}

extension ChatMessagesLoadedAiRun on ChatMessagesLoaded {
  String? get activeAiRunUrl {
    final target = _selectedAiMessage ?? _latestAiMessage;
    if (target == null) return null;
    final content = target.content.trim();
    if (content.isEmpty) return null;
    if (content.startsWith('/')) return 'http://localhost$content';
    return content;
  }

  MessageEntity? get _selectedAiMessage {
    final selectedId = selectedAiRunMessageId;
    if (selectedId == null) return null;
    for (final message in messages) {
      if (message.id != selectedId) continue;
      if (!isAiRunType(message.type)) return null;
      return message.content.trim().isEmpty ? null : message;
    }
    return null;
  }

  MessageEntity? get _latestAiMessage {
    for (final message in messages.reversed) {
      if (!isAiRunType(message.type)) continue;
      if (message.content.trim().isEmpty) continue;
      return message;
    }
    return null;
  }
}

extension ChatMessagesStateAiRun on ChatMessagesState {
  String? get activeAiRunUrl {
    final state = this;
    return state is ChatMessagesLoaded ? state.activeAiRunUrl : null;
  }
}
