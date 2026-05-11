import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';

sealed class ChatHomeState {
  const ChatHomeState();

  ChatEntity? get selectedChat => switch (this) {
    ChatHomeActive(:final chat) => chat,
    ChatHomeLoading(:final previousChat) => previousChat,
    ChatHomeError(:final previousChat) => previousChat,
    ChatHomeInitial() => null,
  };
}

final class ChatHomeInitial extends ChatHomeState {
  const ChatHomeInitial();
}

final class ChatHomeLoading extends ChatHomeState {
  final ChatEntity? previousChat;
  const ChatHomeLoading({this.previousChat});
}

final class ChatHomeActive extends ChatHomeState {
  final ChatEntity chat;
  final bool isNewlyCreated;
  const ChatHomeActive({required this.chat, this.isNewlyCreated = false});
}

final class ChatHomeError extends ChatHomeState {
  final String message;
  final ChatEntity? previousChat;

  const ChatHomeError({required this.message, this.previousChat});
}
