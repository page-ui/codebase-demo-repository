import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';

Future<void> onRenameChatRoom(BuildContext context, ChatEntity chat) async {
  final historyCubit = context.read<ChatHistoryCubit>();
  final homeCubit = context.read<ChatHomeCubit>();
  final controller = TextEditingController(text: chat.name);
  bool didSubmit = false;

  Future<void> submitRename(BuildContext dialogContext, String raw) async {
    if (didSubmit) return;
    didSubmit = true;

    final newName = raw.trim();
    if (newName.isEmpty || newName == chat.name) {
      Navigator.of(dialogContext).pop();
      return;
    }

    final result = await historyCubit.renameChat(
      chatId: chat.id,
      name: newName,
    );
    if (!dialogContext.mounted) return;

    result.fold(
      (failure) {
        didSubmit = false;
        showSnackBar(
          context: dialogContext,
          message: failure.message,
          backgroundColor: AppColors.red,
          textColor: AppColors.white,
        );
      },
      (renamed) {
        homeCubit.updateSelectedChat(
          chat: ChatEntity(
            id: chat.id,
            name: renamed.name,
            createdAt: chat.createdAt,
          ),
        );
        Navigator.of(dialogContext).pop();
      },
    );
  }

  final buttonBox = context.findRenderObject() as RenderBox?;
  final overlayBox =
      Overlay.of(context).context.findRenderObject() as RenderBox?;
  final Rect? anchorRect;
  if (buttonBox == null || overlayBox == null) {
    anchorRect = null;
  } else {
    anchorRect = Rect.fromPoints(
      buttonBox.localToGlobal(Offset.zero, ancestor: overlayBox),
      buttonBox.localToGlobal(
        buttonBox.size.bottomRight(Offset.zero),
        ancestor: overlayBox,
      ),
    );
  }

  await showGeneralDialog<void>(
    context: context,
    barrierDismissible: true,
    barrierLabel: 'Dismiss',
    barrierColor: Colors.black54,
    transitionDuration: const Duration(milliseconds: 120),
    pageBuilder: (dialogContext, animation, secondaryAnimation) {
      final media = MediaQuery.of(dialogContext);
      final overlaySize = overlayBox?.size ?? media.size;
      final historyPanelWidth = MediaQuery.sizeOf(context).width < 270.0 ? MediaQuery.sizeOf(context).width  : 270;

      final double dialogWidth = overlaySize.width <= historyPanelWidth.toDouble() + 24
          ? overlaySize.width - 24
          : historyPanelWidth.toDouble();
      final double left = anchorRect == null
          ? (overlaySize.width - dialogWidth) / 2
          : (anchorRect.right - dialogWidth).clamp(
              12.0,
              overlaySize.width - dialogWidth - 12.0,
            );
      final double top = anchorRect == null
          ? (overlaySize.height * 0.2)
          : (anchorRect.bottom + 8).clamp(12.0, overlaySize.height - 220.0);

      return Material(
        type: MaterialType.transparency,
        child: Stack(
          children: [
            Positioned(
              left: left,
              top: top,
              width: dialogWidth,
              child: Material(
                color: AppColors.primaryColor.withValues(alpha: 0.96),
                surfaceTintColor: AppColors.transparent,
                shadowColor: AppColors.darkGreen,
                elevation: 12,
                shape: RoundedRectangleBorder(
                  borderRadius: AppBorders.xxxs,
                  side: BorderSide(
                    color: AppColors.darkGreen.withValues(alpha: 0.35),
                  ),
                ),
                child: Padding(
                  padding: const EdgeInsets.only(top: 12, bottom: 8),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        child: TextField(
                          controller: controller,
                          autofocus: true,
                          style: const TextStyle(color: AppColors.white),
                          cursorColor: AppColors.white,
                          decoration: InputDecoration(
                            hintText: 'Chat name',
                            hintStyle: TextStyle(
                              color: AppColors.lightGray.withValues(alpha: 0.7),
                            ),
                            enabledBorder: UnderlineInputBorder(
                              borderSide: BorderSide(
                                color: AppColors.lightGray.withValues(
                                  alpha: 0.4,
                                ),
                              ),
                            ),
                            focusedBorder: const UnderlineInputBorder(
                              borderSide: BorderSide(color: AppColors.white),
                            ),
                          ),
                          onSubmitted: (value) =>
                              submitRename(dialogContext, value),
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            TextButton(
                              onPressed: () =>
                                  Navigator.of(dialogContext).pop(),
                              child: const Text(
                                'Cancel',
                                style: TextStyle(color: AppColors.lightGray),
                              ),
                            ),
                            TextButton(
                              onPressed: () {
                                submitRename(dialogContext, controller.text);
                              },
                              child: const Text(
                                'Rename',
                                style: TextStyle(color: AppColors.white),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    },
  );

  controller.dispose();
}
