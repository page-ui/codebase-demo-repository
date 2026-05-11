import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_state.dart';
import 'package:page_ui/features/chat/presentation/widgets/custom_button_icon_for_panels.dart';
import 'package:page_ui/features/chat/presentation/widgets/ui_frame/iframe_view.dart';
import 'package:page_ui/features/chat/presentation/widgets/ui_frame/open_ui_url.dart';

class UIFrameBody extends StatelessWidget {
  const UIFrameBody({super.key, required this.onRightButtonPressed});

  final void Function()? onRightButtonPressed;

  @override
  Widget build(BuildContext context) {
    final isMobile = context.isMobile;

    return BlocSelector<ChatMessagesCubit, ChatMessagesState, String>(
      selector: (state) => state.activeAiRunUrl?.trim() ?? '',
      builder: (context, url) {
        final hasUrl = url.isNotEmpty;

        return Column(
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                AbsorbPointer(
                  absorbing: isMobile ? false : true,
                  child: Opacity(
                    opacity: isMobile ? 1 : 0,
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        CustomButtonIconForPanels(
                          onPressed: onRightButtonPressed,
                        ),
                      ],
                    ),
                  ),
                ),
                Tooltip(
                  message: hasUrl ? 'Open in new tab' : 'No preview available',
                  child: IconButton(
                    onPressed: hasUrl ? () => openUiUrlInBrowser(url) : null,
                    icon: Icon(
                      AppIcons.openInNew,
                      size: 18,
                      color: hasUrl
                          ? AppColors.lightGray.withValues(alpha: 0.9)
                          : AppColors.lightGray.withValues(alpha: 0.35),
                    ),
                  ),
                ),
              ],
            ),
            Expanded(
              child: IframeView(key: ValueKey(url), url: url),
            ),
          ],
        );
      },
    );
  }
}
