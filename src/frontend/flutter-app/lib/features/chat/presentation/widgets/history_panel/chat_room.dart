import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/core/helpers/format_date_time.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/menu_button.dart';
import 'package:flutter/material.dart';

class ChatRoom extends StatelessWidget {
  const ChatRoom({
    super.key,
    required this.chat,
    this.onTap,
    this.onRename,
    this.onDelete,
    this.isSelected = false,
  });

  final ChatEntity chat;
  final VoidCallback? onTap;
  final Future<void> Function(BuildContext menuButtonContext)? onRename;
  final Future<void> Function(BuildContext menuButtonContext)? onDelete;
  final bool isSelected;

  @override
  Widget build(BuildContext context) {
    final hasMenuActions = onRename != null && onDelete != null;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Material(
        color: Colors.transparent,
        child: Ink(
          decoration: BoxDecoration(
            color: isSelected
                ? AppColors.primaryColor.withValues(alpha: 0.2)
                : AppColors.black.withValues(alpha: 0.8),
            borderRadius: AppBorders.xxxs,
            border: Border.all(
              color: isSelected
                  ? AppColors.primaryColor.withValues(alpha: 0.6)
                  : AppColors.white.withValues(alpha: 0.3),
              width: 1.2,
            ),
          ),
          child: InkWell(
            onTap: onTap,
            borderRadius: AppBorders.xxxs,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Padding(
                              padding: const EdgeInsets.only(top: 4, right: 8),
                              child: Icon(
                                AppIcons.arrowForward,
                                size: 12,
                                color: isSelected
                                    ? AppColors.primaryColor
                                    : AppColors.lightGray,
                              ),
                            ),
                            Expanded(
                              child: Text(
                                chat.name,
                                maxLines: 3,
                                overflow: TextOverflow.ellipsis,
                                style: TextStyle(
                                  color: isSelected
                                      ? AppColors.white
                                      : AppColors.lightGray.withValues(
                                          alpha: 0.9,
                                        ),
                                  fontSize: 14,
                                  height: 1.5,
                                  fontWeight: isSelected
                                      ? FontWeight.w500
                                      : null,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        Text(
                          formatDateTime(chat.createdAt ?? DateTime.now()),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: isSelected
                                ? AppColors.white.withValues(alpha: 0.7)
                                : AppColors.white.withValues(alpha: 0.5),
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                if (hasMenuActions)
                  Padding(
                    padding: const EdgeInsets.only(top: 8, right: 8),
                    child: MenuButton(
                      onRename: (ctx) => onRename!(ctx),
                      onDelete: (ctx) => onDelete!(ctx),
                      isSelected: isSelected,
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
