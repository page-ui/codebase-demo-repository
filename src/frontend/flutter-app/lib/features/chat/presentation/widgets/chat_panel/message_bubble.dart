import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/features/chat/domain/constants/message_types.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_state.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_message_content.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class MessageBubble extends StatelessWidget {
  const MessageBubble({super.key, required this.message});

  final MessageEntity message;

  bool get _isAssistant => isAssistantMessage(message.type);

  bool get _isAiRun => isAiRunType(message.type);

  @override
  Widget build(BuildContext context) {
    final isUser = !_isAssistant;

    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        constraints: const BoxConstraints(maxWidth: 310),
        margin: const EdgeInsets.symmetric(vertical: 4),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: isUser
              ? AppColors.primaryColor.withValues(alpha: 0.15)
              : AppColors.anotherGray.withValues(alpha: 0.6),
          borderRadius: AppBorders.xxxxs,
          border: Border.all(
            color: isUser
                ? AppColors.primaryColor.withValues(alpha: 0.3)
                : AppColors.darkGrey.withValues(alpha: 0.5),
            width: 0.5,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SelectionArea(child: ChatMessageContent(message: message)),
            const SizedBox(height: 4),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  _formatTime(message.createdAt),
                  style: TextStyle(
                    color: AppColors.white.withValues(alpha: 0.4),
                    fontSize: 11,
                  ),
                ),
                if (_isAiRun) ...[
                  const SizedBox(width: 6),
                  _PreviewUiButton(messageId: message.id),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  String _formatTime(DateTime dateTime) {
    final hour = dateTime.hour.toString().padLeft(2, '0');
    final minute = dateTime.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }
}

class _PreviewUiButton extends StatelessWidget {
  const _PreviewUiButton({required this.messageId});

  final String messageId;

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ChatMessagesCubit, ChatMessagesState>(
      buildWhen: (prev, curr) {
        String? prevSelected;
        String? currSelected;
        String? chatId;
        if (prev is ChatMessagesLoaded) {
          prevSelected = prev.selectedAiRunMessageId;
        }
        if (curr is ChatMessagesLoaded) {
          currSelected = curr.selectedAiRunMessageId;
          chatId = curr.chatId;
        }
        return prevSelected != currSelected || chatId == null;
      },
      builder: (context, state) {
        final isSelected =
            state is ChatMessagesLoaded &&
            state.selectedAiRunMessageId == messageId;
        if (state is! ChatMessagesLoaded) {
          return const SizedBox.shrink();
        }
        final chatId = state.chatId;
        return InkWell(
          onTap: () {
            context.read<ChatMessagesCubit>().selectAiRunMessage(
              chatId: chatId,
              messageId: messageId,
            );
          },
          borderRadius: AppBorders.xxxxs,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  isSelected ? Icons.visibility : Icons.visibility_outlined,
                  size: 13,
                  color: AppColors.primaryColor.withValues(
                    alpha: isSelected ? 1 : 0.7,
                  ),
                ),
                const SizedBox(width: 4),
                Text(
                  isSelected ? 'Viewing' : 'Preview',
                  style: TextStyle(
                    color: AppColors.primaryColor.withValues(
                      alpha: isSelected ? 1 : 0.8,
                    ),
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
