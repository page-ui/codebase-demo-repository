import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/features/chat/presentation/widgets/name_and_the_logo.dart';
import 'package:page_ui/features/chat/presentation/widgets/settings_menu_button.dart';

class HomeAppbar extends StatelessWidget {
  const HomeAppbar({super.key, this.onHistoryPressed});

  final VoidCallback? onHistoryPressed;

  @override
  Widget build(BuildContext context) {
    return AppBar(
      automaticallyImplyLeading: false,
      toolbarHeight: 45,
      backgroundColor: AppColors.anotherGray.withValues(alpha: 0.8),
      title: Row(
        children: [
          IconButton(
            onPressed: onHistoryPressed,
            tooltip: 'History',
            icon: const Icon(
              AppIcons.listIcon,
              color: AppColors.grey,
              size: 20,
            ),
          ),
          const NameAndTheLogo(),
        ],
      ),
      actions: const [
        Padding(
          padding: EdgeInsets.only(right: 16.0),
          child: SettingsMenuButton(),
        ),
      ],
    );
  }
}
