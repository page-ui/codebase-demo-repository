import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/core/helpers/panel_scrollbar.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/chat_room.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/chat_rooms_loading_indicators.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/on_delete_chat_room_function.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/on_rename_chat_room_function.dart';

class ListOfChatRooms extends StatefulWidget {
  const ListOfChatRooms({
    super.key,
    required this.searchController,
    this.debounce,
    this.onChatSelected,
  });
  final TextEditingController searchController;
  final Timer? debounce;
  final VoidCallback? onChatSelected;
  @override
  State<ListOfChatRooms> createState() => _ListOfChatRoomsState();
}

class _ListOfChatRoomsState extends State<ListOfChatRooms> {
  final _scrollController = ScrollController();

  void _onChatSelected(ChatEntity chat) {
    context.read<ChatHomeCubit>().selectChat(chat: chat);

    if (context.isMobile || context.isTablet) {
      widget.onChatSelected?.call();
    }
  }

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 100) {
      context.read<ChatHistoryCubit>().loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: BlocBuilder<ChatHistoryCubit, ChatHistoryState>(
        builder: (context, state) {
          if (state is ChatHistoryLoading) {
            return const ChatRoomsLoadingIndicators();
          }

          if (state is ChatHistoryFailure) {
            return FailureMessageWidget(message: state.message,);
          }

          if (state is ChatHistoryLoaded) {
            if (state.chats.isEmpty) {
              return const NoChatsYetWidget();
            }

            return PanelScrollbar(
              controller: _scrollController,
              child: BlocBuilder<ChatHomeCubit, ChatHomeState>(
                builder: (context, homeState) {
                  final selectedChatId = homeState.selectedChat?.id;

                  return ListView.builder(
                    controller: _scrollController,
                    padding: const EdgeInsets.only(right: 16),
                    itemCount:
                        state.chats.length + (state.isLoadingMore ? 1 : 0),
                    itemBuilder: (context, index) {
                      if (index == state.chats.length) {
                        return const ChatRoomsLoadingIndicators();
                      }
                      final chat = state.chats[index];
                      return ChatRoom(
                        chat: chat,
                        isSelected: chat.id == selectedChatId,
                        onTap: () => _onChatSelected(chat),
                        onRename: (menuButtonContext) =>
                            onRenameChatRoom(menuButtonContext, chat),
                        onDelete: (menuButtonContext) =>
                            onDeleteChatRoom(menuButtonContext, chat),
                      );
                    },
                  );
                },
              ),
            );
          }

          return const SizedBox.shrink();
        },
      ),
    );
  }
}

class FailureMessageWidget extends StatelessWidget {
  const FailureMessageWidget({super.key, required this.message});
  final String message;
  @override
  Widget build(BuildContext context) {
    return Center(
      child: Text(
        message,
        style: TextStyle(
          color: AppColors.lightGray.withValues(alpha: 0.6),
          fontSize: 13,
        ),
        textAlign: TextAlign.center,
      ),
    );
  }
}

class NoChatsYetWidget extends StatelessWidget {
  const NoChatsYetWidget({super.key});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Text(
        'No chats yet',
        style: TextStyle(
          color: AppColors.lightGray.withValues(alpha: 0.5),
          fontSize: 13,
        ),
      ),
    );
  }
}
