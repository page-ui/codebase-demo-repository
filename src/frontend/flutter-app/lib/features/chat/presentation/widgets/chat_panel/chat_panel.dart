import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_input_builder.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_panel_header.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/load_messages_builder.dart';

class ChatPanel extends StatefulWidget {
  const ChatPanel({super.key, required this.onPressed});
  final void Function()? onPressed;

  @override
  State<ChatPanel> createState() => _ChatPanelState();
}

class _ChatPanelState extends State<ChatPanel> {
  ChatMessagesCubit? _messagesCubit;
  String? _openedChatId;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      _openSelectedChat();
    });
  }

  void _openSelectedChat() {
    final homeState = context.read<ChatHomeCubit>().state;
    final chatId = homeState.selectedChat?.id;
    if (chatId == null) return;

    _messagesCubit = context.read<ChatMessagesCubit>();
    _openedChatId = chatId;

    
    if (homeState is ChatHomeActive && homeState.isNewlyCreated) return;

    _messagesCubit!.openChat(chatId: chatId);
  }

  @override
  void dispose() {
    final cubit = _messagesCubit;
    final chatId = _openedChatId;
    if (cubit != null && chatId != null) {
      cubit.closeChat(chatId: chatId);
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final selectedChat = context.read<ChatHomeCubit>().state.selectedChat;
    if (selectedChat == null) {
      return const SizedBox.shrink();
    }

    return BlocListener<ChatHomeCubit, ChatHomeState>(
      listenWhen: (previous, current) =>
          previous.selectedChat?.id != current.selectedChat?.id &&
          current.selectedChat != null,
      listener: (context, state) {
        final chatId = state.selectedChat?.id;
        if (chatId == null) return;
        _messagesCubit = context.read<ChatMessagesCubit>();
        _openedChatId = chatId;

        
        if (state is ChatHomeActive && state.isNewlyCreated) return;

        _messagesCubit!.openChat(chatId: chatId);
      },
      child: Padding(
        padding: EdgeInsets.only(top: context.isMobile?0: 10),
        child: Column(
          children: [
            ChatPanelHeader(onPressed: widget.onPressed),
            const SizedBox(height: 8),
            const LoadMessagesBuilder(),
            const SizedBox(height: 8),
            const ChatInputBuilder(),
          ],
        ),
      ),
    );
  }
}
