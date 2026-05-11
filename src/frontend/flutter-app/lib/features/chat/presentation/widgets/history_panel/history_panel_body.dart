import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_cubit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_home_cubit/chat_home_state.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/history_panel_header.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/history_search_text_field.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/list_of_chat_rooms.dart';

class HistoryPanelBody extends StatefulWidget {
  const HistoryPanelBody({super.key, this.onPressed});
  final void Function()? onPressed;

  @override
  State<HistoryPanelBody> createState() => _HistoryPanelBodyState();
}

class _HistoryPanelBodyState extends State<HistoryPanelBody> {
  final _searchController = TextEditingController();
  Timer? _debounce;

  void _onSearchChanged(String query) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () {
      if (!mounted) return;
      context.read<ChatHistoryCubit>().searchChats(query: query);
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right:  10, left: 10, top: 8, bottom: 4),
      child: Column(
        children: [
          HistoryPanelHeader(widget: widget),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: BlocBuilder<ChatHomeCubit, ChatHomeState>(
              builder: (context, state) {
                return AbsorbPointer(
                  absorbing: state is ChatHomeLoading,
                  child: TextButton.icon(
                    style: TextButton.styleFrom(
                      foregroundColor: AppColors.textGray,
                      backgroundColor: AppColors.black.withValues(alpha: 0.3),
                      alignment: Alignment.centerLeft,
                      padding: const EdgeInsets.symmetric(
                        vertical: 12,
                        horizontal: 12,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                        side: BorderSide(
                          color: AppColors.darkGrey.withValues(alpha: 0.3),
                        ),
                      ),
                    ),
                    onPressed: () {
                      context.read<ChatHomeCubit>().reset();
                      widget.onPressed?.call();
                    },
                    icon: const Icon(AppIcons.plus),
                    label: const Text('Create New Chat'),
                  ),
                );
              },
            ),
          ),
          const SizedBox(height: 8),
          HistorySearchTextField(
            onSearch: _onSearchChanged,
            searchController: _searchController,
          ),
          const SizedBox(height: 12),
          ListOfChatRooms(
            searchController: _searchController,
            debounce: _debounce,
            onChatSelected: widget.onPressed,
          ),
        ],
      ),
    );
  }
}
