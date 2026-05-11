import 'package:flutter/material.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_panel.dart';
import 'package:page_ui/features/chat/presentation/widgets/custom_animated_container_for_the_home_panel.dart';
import 'package:page_ui/features/chat/presentation/widgets/ui_frame/u_i_frame.dart';

class DesktopHomeLayout extends StatelessWidget {
  final bool isChatOpen;
  final VoidCallback onToggleChatPanel;

  const DesktopHomeLayout({
    super.key,
    required this.isChatOpen,
    required this.onToggleChatPanel,
  });

  @override
  Widget build(BuildContext context) {
    final chatWidth = context.isTablet ? 380.0 : 480.0;

    return Row(
      children: [
        CustomAnimatedContainerForTheHomePanel(
          isOpen: isChatOpen,
          width: chatWidth,
          onPressed: onToggleChatPanel,
          child: ChatPanel(onPressed: onToggleChatPanel),
        ),
        Expanded(
          child: UIFrame(
            wrapWithExpanded: false,
            onRightButtonPressed: onToggleChatPanel,
          ),
        ),
      ],
    );
  }
}
