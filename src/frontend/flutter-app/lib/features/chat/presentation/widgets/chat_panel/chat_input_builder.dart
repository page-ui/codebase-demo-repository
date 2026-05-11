import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/chat/domain/params/send_message_params.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/pick_file_cubit/pick_file_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/send_message_cubit/send_message_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/send_message_cubit/send_message_state.dart';
import 'package:page_ui/features/chat/presentation/widgets/chat_panel/chat_input_bar.dart';

class ChatInputBuilder extends StatefulWidget {
  const ChatInputBuilder({super.key, this.onSend});

  
  
  
  final void Function(String content)? onSend;

  bool get isLandingMode => onSend != null;

  @override
  State<ChatInputBuilder> createState() => _ChatInputBuilderState();
}

class _ChatInputBuilderState extends State<ChatInputBuilder> {
  final TextEditingController _controller = TextEditingController();
  final FocusNode _focusNode = FocusNode();
  String _lastSentContent = '';

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => getit.get<SendMessageCubit>(),
      child: BlocListener<SendMessageCubit, SendMessageState>(
        listener: (context, state) {
          final pickFileCubit = context.read<PickFileCubit>();

          if (state is SendMessageSuccess) {
            _controller.clear();
            _focusNode.requestFocus();

            if (pickFileCubit.isImagePicked) {
              pickFileCubit.removeImage();
            }
          }

          if (state is SendMessageError) {
            showSnackBar(
              context: context,
              message: state.message,
              backgroundColor: AppColors.red,
              textColor: AppColors.white,
            );
          }
        },
        child: BlocSelector<SendMessageCubit, SendMessageState, bool>(
          selector: (state) => state is SendMessageLoading,
          builder: (context, isSendLoading) {
            return BlocSelector<ChatMessagesCubit, ChatMessagesState, bool>(
              selector: (state) {
                
                if (widget.isLandingMode) return false;
                return state is ChatMessagesLoaded &&
                    (state.isAwaitingAiResponse ||
                        state.activeThinkingMessage != null ||
                        state.isSubscriptionActive);
              },
              builder: (context, isAwaitingAi) {
                return BlocBuilder<ChatHomeCubit, ChatHomeState>(
                  builder: (context, homeState) {
                    final isCreatingChat = homeState is ChatHomeLoading;
                    final isSending =
                        isSendLoading || isAwaitingAi || isCreatingChat;

                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8.0, left: 8, right: 8),
                      child: ChatInputBar(
                        controller: _controller,
                        focusNode: _focusNode,
                        isSending: isSending,
                        onSend: () => _handleSend(context),
                      ),
                    );
                  },
                );
              },
            );
          },
        ),
      ),
    );
  }

  void _handleSend(BuildContext context) {
    final sendMessageCubit = context.read<SendMessageCubit>();
    if (sendMessageCubit.state is SendMessageLoading) return;

    final pickFileCubit = context.read<PickFileCubit>();
    final message = _controller.text;
    if (message.isEmpty && !pickFileCubit.isImagePicked) return;

    
    
    _lastSentContent = message.trim();

    if (widget.onSend != null) {
      widget.onSend!(message.isEmpty ? 'image' : message);
      return;
    }

    final homeState = context.read<ChatHomeCubit>().state;
    final selectedChat = homeState.selectedChat;
    if (selectedChat == null) return;

    if (pickFileCubit.isImagePicked) {
      context.read<SendMessageCubit>().setImageData(
        bytes: pickFileCubit.imageBytes!,
        fileName: pickFileCubit.imageFileName!,
        contentType: pickFileCubit.imageContentType!,
      );
    }

    final messagesCubit = context.read<ChatMessagesCubit>();

    
    if (_lastSentContent.isNotEmpty) {
      messagesCubit.addOutgoingMessage(
        chatId: selectedChat.id,
        content: _lastSentContent,
      );
    }

    
    messagesCubit.startMessageSubscription(selectedChat.id);

    sendMessageCubit.sendMessage(
      params: SendMessageParams(
        chatId: selectedChat.id,
        content: message.isEmpty ? 'image' : message,
      ),
    );
  }

  
  
  
}
