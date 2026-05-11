import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';

sealed class ChatHistoryState {
  const ChatHistoryState();
}

final class ChatHistoryInitial extends ChatHistoryState {
  const ChatHistoryInitial();
}

final class ChatHistoryLoading extends ChatHistoryState {
  const ChatHistoryLoading();
}

final class ChatHistoryLoaded extends ChatHistoryState {
  final List<ChatEntity> chats;
  final bool hasNextPage;
  final String? endCursor;
  final bool isLoadingMore;

  const ChatHistoryLoaded({
    required this.chats,
    required this.hasNextPage,
    this.endCursor,
    this.isLoadingMore = false,
  });

  ChatHistoryLoaded copyWith({
    List<ChatEntity>? chats,
    bool? hasNextPage,
    String? endCursor,
    bool? isLoadingMore,
  }) {
    return ChatHistoryLoaded(
      chats: chats ?? this.chats,
      hasNextPage: hasNextPage ?? this.hasNextPage,
      endCursor: endCursor ?? this.endCursor,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    );
  }
}

final class ChatHistoryFailure extends ChatHistoryState {
  final String message;
  const ChatHistoryFailure({required this.message});
}
