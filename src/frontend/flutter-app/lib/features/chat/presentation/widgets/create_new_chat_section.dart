import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_input_builder.dart';
import 'package:flutter/material.dart';

class CreateNewChatSection extends StatelessWidget {
  const CreateNewChatSection({
    super.key,
    required this.onSend,
    this.errorMessage,
  });

  final Function({required String content, String? attachmentUrl}) onSend;
  final String? errorMessage;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.transparent,
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                AppIcons.chatBubble,
                size: 64,
                color: AppColors.primaryColor.withValues(alpha: 0.6),
              ),
              const SizedBox(height: 24),
              Text(
                'Welcome to Page.ui',
                style: AppTextStyles.headlineSmall?.copyWith(
                  color: AppColors.white,
                  letterSpacing: 1.5,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Start a conversation to generate UI',
                style: AppTextStyles.bodyLarge?.copyWith(
                  color: AppColors.lightGray.withValues(alpha: 0.7),
                ),
              ),
              const SizedBox(height: 32),
              ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 600),
                child: ChatInputBuilder(
                  onSend: (content) {
                    onSend(content: content);
                  },
                ),
              ),
              if (errorMessage != null) ...[
                const SizedBox(height: 16),
                Text(
                  errorMessage!,
                  style: AppTextStyles.bodyMedium?.copyWith(
                    color: AppColors.lightRed,
                  ),
                  textAlign: TextAlign.center,
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
