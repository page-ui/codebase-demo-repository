import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/features/chat/domain/entities/chat_entity.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/pick_file_cubit/pick_file_cubit.dart';
import 'package:page_ui/features/chat/presentation/widgets/home_view_body.dart';

mixin HomeViewBodyMixin on State<HomeViewBody> {
  bool isHistoryOpen = false;
  bool isChatOpen = true;
  final PageController pageController = PageController();

  @override
  void dispose() {
    pageController.dispose();
    super.dispose();
  }

  void updateUrlForChat(ChatEntity chat) {
    final targetPath = '/app/chat/${chat.name}';
    SystemNavigator.routeInformationUpdated(
      uri: Uri.parse(targetPath),
      replace: true,
    );
  }

  void toggleHistory() => setState(() => isHistoryOpen = !isHistoryOpen);

  void toggleChatPanel() {
    if (context.isMobile) {
      animateToPage(0);
    } else {
      setState(() => isChatOpen = !isChatOpen);
    }
  }

  void showUIFrame() {
    if (context.isMobile) animateToPage(1);
  }

  void animateToPage(int page) {
    if (!pageController.hasClients) return;
    pageController.animateToPage(
      page,
      duration: const Duration(milliseconds: 250),
      curve: Curves.easeInOut,
    );
  }

  Future<void> onSendFromLanding(BuildContext context, String content) async {
    final pickFileCubit = context.read<PickFileCubit>();
    final chatHomeCubit = context.read<ChatHomeCubit>();

    await chatHomeCubit.createChatWithPicker(
      content: content,
      pickFileCubit: pickFileCubit,
    );

    if (!context.mounted) return;

    
    
    final chatId = chatHomeCubit.state.selectedChat?.id;
    if (chatId != null) {
      final messagesCubit = context.read<ChatMessagesCubit>();
      
      
      messagesCubit.addOutgoingMessage(
        chatId: chatId,
        content: content,
        attachmentUrl: pickFileCubit.isImagePicked ? null : null,
      );
      messagesCubit.startMessageSubscription(chatId);
    }
  }

  void onHomeStateChanged(BuildContext context, ChatHomeState state) {
    if (state is ChatHomeError) {
      showSnackBar(
        context: context,
        message: state.message,
        backgroundColor: AppColors.red,
        textColor: AppColors.white,
      );
    }

    if (state is ChatHomeActive) {
      setState(() {
        isChatOpen = true;
        isHistoryOpen = false;
      });
      if (context.isMobile && pageController.hasClients) {
        pageController.jumpToPage(0);
      }
      updateUrlForChat(state.chat);
    }
  }
}
