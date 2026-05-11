import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';

Future<void> onDeleteChatRoom(BuildContext context, ChatEntity chat) async {
  final historyCubit = context.read<ChatHistoryCubit>();
  final homeCubit = context.read<ChatHomeCubit>();
  final messagesCubit = context.read<ChatMessagesCubit>();

  final confirmed = await showDialog<bool>(
    context: context,
    builder: (dialogContext) => SimpleDialog(
      backgroundColor: AppColors.primaryColor.withValues(alpha: 0.96),
      surfaceTintColor: AppColors.transparent,
      shadowColor: AppColors.darkGreen,
      shape: RoundedRectangleBorder(
        borderRadius: AppBorders.xxxs,
        side: BorderSide(color: AppColors.darkGreen.withValues(alpha: 0.35)),
      ),
      title: const Text(
        'Delete chat',
        style: TextStyle(color: AppColors.white),
      ),
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Text(
            'Are you sure you want to delete "${chat.name}"?',
            style: const TextStyle(color: AppColors.lightGray),
          ),
        ),
        const SizedBox(height: 12),
        Padding(
          padding: const EdgeInsets.only(right: 12, bottom: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: () => Navigator.of(dialogContext).pop(false),
                child: const Text(
                  'Cancel',
                  style: TextStyle(color: AppColors.lightGray),
                ),
              ),
              TextButton(
                onPressed: () => Navigator.of(dialogContext).pop(true),
                child: const Text(
                  'Delete',
                  style: TextStyle(color: AppColors.white),
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );

  if (confirmed != true || !context.mounted) return;

  final wasSelected = homeCubit.state.selectedChat?.id == chat.id;
  final currentSelectedId = homeCubit.state.selectedChat?.id;

  final result = await historyCubit.deleteChat(chatId: chat.id);
  if (!context.mounted) return;

  result.fold(
    (failure) => showSnackBar(
      context: context,
      message: failure.message,
      backgroundColor: AppColors.red,
      textColor: AppColors.white,
    ),
    (_) {
      if (wasSelected) {
        homeCubit.reset();
      } else if (currentSelectedId != null) {
        messagesCubit.refreshMessages(chatId: currentSelectedId);
      }
    },
  );
}
