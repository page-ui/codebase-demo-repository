import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/chat_room.dart';
import 'package:flutter/material.dart';
import 'package:skeletonizer/skeletonizer.dart';

class ChatRoomsLoadingIndicators extends StatelessWidget {
  const ChatRoomsLoadingIndicators({super.key});

  static const _placeholderChats = [
    ChatEntity(id: 'chat-room-loading-1', name: 'Loading chat preview'),
    ChatEntity(id: 'chat-room-loading-2', name: 'Loading chat preview'),
    ChatEntity(id: 'chat-room-loading-3', name: 'Loading chat preview'),
  ];

  @override
  Widget build(BuildContext context) {
    return Skeletonizer(
      child: ListView.builder(
        itemCount: _placeholderChats.length,
        itemBuilder: (context, index) {
          return ChatRoom(chat: _placeholderChats[index]);
        },
      ),
    );
  }
}
