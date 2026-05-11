import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:flutter/material.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

enum _ChatRoomAction { rename, delete }

class MenuButton extends StatelessWidget {
  const MenuButton({
    required this.onRename,
    required this.onDelete,
    required this.isSelected,
  });

  final Future<void> Function(BuildContext menuButtonContext) onRename;
  final Future<void> Function(BuildContext menuButtonContext) onDelete;
  final bool isSelected;

  @override
  Widget build(BuildContext context) {
    final iconColor = isSelected
        ? AppColors.white
        : AppColors.lightGray.withValues(alpha: 0.8);

    Future<void> openActionsDialog() async {
      final buttonBox = context.findRenderObject() as RenderBox?;
      final overlayBox =
          Overlay.of(context).context.findRenderObject() as RenderBox?;
      if (buttonBox == null || overlayBox == null) return;

      final rect = Rect.fromPoints(
        buttonBox.localToGlobal(Offset.zero, ancestor: overlayBox),
        buttonBox.localToGlobal(
          buttonBox.size.bottomRight(Offset.zero),
          ancestor: overlayBox,
        ),
      );

      final action = await showMenu<_ChatRoomAction>(
        context: context,
        position: RelativeRect.fromRect(rect, Offset.zero & overlayBox.size),
        color: AppColors.primaryColor.withValues(alpha: 0.96),
        shape: RoundedRectangleBorder(
          borderRadius: AppBorders.xxxs,
          side: BorderSide(color: AppColors.darkGreen.withValues(alpha: 0.35)),
        ),
        items: const [
          PopupMenuItem(
            value: _ChatRoomAction.rename,
            child: Row(
              children: [
                Icon(Icons.edit_outlined, size: 16, color: AppColors.white),
                SizedBox(width: 8),
                Text(
                  'Rename',
                  style: TextStyle(color: AppColors.white, fontSize: 13),
                ),
              ],
            ),
          ),
          PopupMenuItem(
            value: _ChatRoomAction.delete,
            child: Row(
              children: [
                Icon(Icons.delete_outline, size: 16, color: AppColors.white),
                SizedBox(width: 8),
                Text(
                  'Delete',
                  style: TextStyle(color: AppColors.white, fontSize: 13),
                ),
              ],
            ),
          ),
        ],
      );

      if (!context.mounted) return;
      if (action == _ChatRoomAction.rename) {
        await onRename(context);
      } else if (action == _ChatRoomAction.delete) {
        await onDelete(context);
      }
    }

    return SizedBox(
      height: 24,
      width: 24,
      child: PointerInterceptor(
        child: IconButton(
          tooltip: 'More',
          padding: EdgeInsets.zero,
          onPressed: openActionsDialog,
          icon: Icon(Icons.more_vert, size: 18, color: iconColor),
        ),
      ),
    );
  }
}
