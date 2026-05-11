import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_panel.dart';
import 'package:page_ui/features/chat/presentation/widgets/ui_frame/u_i_frame.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

class MobileHomeLayout extends StatelessWidget {
  final PageController pageController;
  final VoidCallback onShowUIFrame;
  final VoidCallback onToggleChatPanel;

  const MobileHomeLayout({
    super.key,
    required this.pageController,
    required this.onShowUIFrame,
    required this.onToggleChatPanel,
  });

  @override
  Widget build(BuildContext context) {
    return PointerInterceptor(
      intercepting: false,
      child: PageView(
        controller: pageController,
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          Container(
            decoration: BoxDecoration(
              borderRadius: AppBorders.zero,
              color: AppColors.anotherGray.withValues(alpha: 0.6),
              border: Border.all(color: AppColors.darkGrey, width: 0.5),
            ),
            child: ChatPanel(onPressed: onShowUIFrame),
          ),
          UIFrame(
            wrapWithExpanded: false,
            onRightButtonPressed: onToggleChatPanel,
          ),
        ],
      ),
    );
  }
}
