import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_history_cubit/chat_history_cubit.dart';
import 'package:page_ui/features/chat/presentation/widgets/history_panel/history_panel_body.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class HistoryPanel extends StatelessWidget {
  const HistoryPanel({super.key, required this.onPressed});
  final void Function()? onPressed;

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => getit.get<ChatHistoryCubit>()..loadChats(),
      child: HistoryPanelBody(onPressed: onPressed),
    );
  }
}
