import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/helpers/panel_scrollbar.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_state.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/message_bubble.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/thinking_bubble.dart';

class LoadMessagesBuilder extends StatefulWidget {
  const LoadMessagesBuilder({super.key});

  @override
  State<LoadMessagesBuilder> createState() => _LoadMessagesBuilderState();
}

class _LoadMessagesBuilderState extends State<LoadMessagesBuilder> {
  final ScrollController _scrollController = ScrollController();
  bool _showScrollToBottom = false;

  static const double _scrollToBottomThreshold = 200;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;

    final pixels = _scrollController.position.pixels;
    final shouldShow = pixels > _scrollToBottomThreshold;
    if (shouldShow != _showScrollToBottom) {
      setState(() => _showScrollToBottom = shouldShow);
    }

    final state = context.read<ChatMessagesCubit>().state;
    if (state is! ChatMessagesLoaded ||
        state.isLoadingMore ||
        !state.hasNextPage) {
      return;
    }

    final maxScroll = _scrollController.position.maxScrollExtent;
    if (pixels >= maxScroll * 0.7) {
      context.read<ChatMessagesCubit>().loadMoreMessages(chatId: state.chatId);
    }
  }

  void _scrollToBottom() {
    if (!_scrollController.hasClients) return;
    _scrollController.animateTo(
      0,
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeOut,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Stack(
        children: [
          PanelScrollbar(controller: _scrollController, child: _buildList()),
          if (_showScrollToBottom)
            Positioned(
              right: 24,
              bottom: 16,
              child: Material(
                color: AppColors.white.withValues(alpha: 0.12),
                shape:  const CircleBorder(),
                child: InkWell(
                  customBorder: const CircleBorder(),
                  onTap: _scrollToBottom,
                  child: const Padding(
                    padding: EdgeInsets.all(8),
                    child: Icon(
                      Icons.arrow_downward,
                      color: AppColors.white,
                      size: 20,
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildList() {
    return BlocBuilder<ChatMessagesCubit, ChatMessagesState>(
      builder: (context, state) {
        if (state is ChatMessagesLoading) {
          return Center(
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: AppColors.white.withValues(alpha: 0.8),
            ),
          );
        }

        if (state is ChatMessagesError) {
          return Center(
            child: Text(
              state.message,
              style: TextStyle(
                color: AppColors.lightGray.withValues(alpha: 0.6),
                fontSize: 13,
              ),
              textAlign: TextAlign.center,
            ),
          );
        }

        if (state is! ChatMessagesLoaded) {
          return const SizedBox.shrink();
        }

        final messages = state.messages;

        if (messages.isEmpty && state.activeThinkingMessage == null) {
          return Center(
            child: Text(
              'Messages will appear here',
              style: TextStyle(
                color: AppColors.lightGray.withValues(alpha: 0.5),
                fontSize: 14,
              ),
            ),
          );
        }

        final showThinking = state.isAwaitingAiResponse || state.activeThinkingMessage != null;
        final thinkingOffset = showThinking ? 1 : 0;
        final loadMoreOffset = state.isLoadingMore ? 1 : 0;
        return ListView.builder(
          controller: _scrollController,
          reverse: true,
          padding: const EdgeInsets.only(top: 4, bottom: 8, right: 16, left: 16),
          itemCount: messages.length + thinkingOffset + loadMoreOffset,
          itemBuilder: (context, index) {
            if (thinkingOffset == 1 && index == 0) {
              return ThinkingBubble(
                label: state.activeThinkingMessage?.content ?? 'Thinking...',
              );
            }
            final messageIndex = index - thinkingOffset;
            if (messageIndex == messages.length) {
              return const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(
                  child: SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppColors.white,
                    ),
                  ),
                ),
              );
            }
            final reversedIndex = messages.length - 1 - messageIndex;
            return MessageBubble(message: messages[reversedIndex]);
          },
        );
      },
    );
  }
}
