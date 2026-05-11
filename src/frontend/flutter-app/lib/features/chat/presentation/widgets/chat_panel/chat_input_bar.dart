import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/image_preview_chat_input_bar.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/pick_image_button.dart';

class ChatInputBar extends StatelessWidget {
  const ChatInputBar({
    super.key,
    required this.controller,
    required this.focusNode,
    required this.onSend,
    required this.isSending,
  });

  final TextEditingController controller;
  final FocusNode focusNode;
  final void Function() onSend;
  final bool isSending;

  void _handleSend() {
    if (isSending) return;
    onSend();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: AppColors.black,
        borderRadius: AppBorders.xxxs,
        border: Border.all(color: AppColors.darkGrey, width: 0.5),
      ),
      child: Column(
        children: [
          const ImagePreviewChatInputBar(),
          Row(
            children: [
              const PickImageButton(),
              const SizedBox(width: 6),
              Expanded(
                child: TextField(
                  keyboardType: TextInputType.multiline,
                  controller: controller,
                  focusNode: focusNode,
                  textInputAction: TextInputAction.newline,
                  style: TextStyle(
                    color: AppColors.white.withValues(alpha: 0.9),
                    fontSize: 14,
                  ),
                  maxLines: 6,
                  minLines: 1,
                  decoration: InputDecoration(
                    hintText: 'Type your prompt...',
                    hintStyle: TextStyle(
                      color: AppColors.lightGray.withValues(alpha: 0.4),
                      fontSize: 14,
                    ),
                    border: InputBorder.none,
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 4,
                      vertical: 4,
                    ),
                    isDense: true,
                  ),
                ),
              ),
              const SizedBox(width: 6),
              AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                decoration: BoxDecoration(
                  color: isSending
                      ? AppColors.primaryColor.withValues(alpha: 0.3)
                      : AppColors.primaryColor.withValues(alpha: 0.8),
                  borderRadius: AppBorders.xxxxs,
                ),
                child: IconButton(
                  onPressed: isSending ? null : _handleSend,
                  icon: isSending
                      ? SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: AppColors.white.withValues(alpha: 0.7),
                          ),
                        )
                      : const Icon(Icons.send_rounded, size: 18),
                  color: AppColors.white,
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(
                    minWidth: 30,
                    minHeight: 30,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
