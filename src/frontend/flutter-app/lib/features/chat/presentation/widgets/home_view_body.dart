import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_messages_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/pick_file_cubit/pick_file_cubit.dart';
import 'package:page_ui/features/chat/presentation/widgets/create_new_chat_section.dart';
import 'package:page_ui/features/chat/presentation/widgets/desktop_home_layout.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/history_panel.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/history_panel_overlay.dart';
import 'package:page_ui/features/chat/presentation/widgets/home_appbar.dart';
import 'package:page_ui/features/chat/presentation/widgets/home_view_body_mixin.dart';
import 'package:page_ui/features/chat/presentation/widgets/home_view_loading_overlay.dart';
import 'package:page_ui/features/chat/presentation/widgets/mobile_home_layout.dart';

class HomeViewBody extends StatefulWidget {
  const HomeViewBody({super.key});

  @override
  State<HomeViewBody> createState() => _HomeViewBodyState();
}

class _HomeViewBodyState extends State<HomeViewBody> with HomeViewBodyMixin {
  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider(create: (_) => PickFileCubit()),
        BlocProvider(create: (_) => getit.get<ChatMessagesCubit>()),
      ],
      child: BlocConsumer<ChatHomeCubit, ChatHomeState>(
        listenWhen: (prev, curr) {
          if (curr is ChatHomeError) return true;
          if (curr is ChatHomeActive) {
            return prev.selectedChat?.id != curr.chat.id;
          }
          return false;
        },
        listener: onHomeStateChanged,
        builder: (context, state) {
          return Stack(
            children: [
              Column(
                children: [
                  HomeAppbar(onHistoryPressed: toggleHistory),
                  Expanded(
                    child: Stack(
                      children: [
                        _buildContent(context, state),
                        if (state is ChatHomeLoading) const HomeViewLoadingOverlay(),
                      ],
                    ),
                  ),
                ],
              ),
              if (isHistoryOpen)
                HistoryPanelOverlay(
                  width: MediaQuery.sizeOf(context).width < 300
                      ? MediaQuery.sizeOf(context).width
                      : 310,
                  panel: HistoryPanel(onPressed: toggleHistory),
                  onClose: () => setState(() => isHistoryOpen = false),
                ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildContent(BuildContext context, ChatHomeState state) {
    final hasChat = state.selectedChat != null;

    if (!hasChat) {
      return CreateNewChatSection(
        onSend: ({required content, attachmentUrl}) =>
            onSendFromLanding(context, content),
      );
    }

    return context.isMobile
        ? MobileHomeLayout(
            pageController: pageController,
            onShowUIFrame: showUIFrame,
            onToggleChatPanel: toggleChatPanel,
          )
        : DesktopHomeLayout(
            isChatOpen: isChatOpen,
            onToggleChatPanel: toggleChatPanel,
          );
  }
}
