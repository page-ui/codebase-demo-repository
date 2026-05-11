import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/helpers/auth_state.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/presentation/controllers/delete_account_cubit/delete_account_cubit.dart';
import 'package:page_ui/features/auth/presentation/controllers/settings_cubit/settings_cubit.dart';
import 'package:pointer_interceptor/pointer_interceptor.dart';

enum _SettingsAction { signOut, deleteAccount }

class SettingsMenuButton extends StatelessWidget {
  const SettingsMenuButton({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider(
          create: (context) => SettingsCubit(getit.get<AuthRepoImpl>()),
        ),
        BlocProvider(
          create: (context) => DeleteAccountCubit(getit.get<AuthRepoImpl>()),
        ),
      ],
      child: const _SettingsMenuButtonContent(),
    );
  }
}

class _SettingsMenuButtonContent extends StatelessWidget {
  const _SettingsMenuButtonContent();

  @override
  Widget build(BuildContext context) {
    Future<void> openActionsDialog() async {
      final buttonBox = context.findRenderObject() as RenderBox?;
      final overlayBox =
          Overlay.of(context).context.findRenderObject() as RenderBox?;
      if (buttonBox == null || overlayBox == null) return;

      final rect = Rect.fromPoints(
        buttonBox.localToGlobal(Offset.zero, ancestor: overlayBox),
        buttonBox.localToGlobal(
          buttonBox.size.bottomRight(Offset.zero),
          ancestor: overlayBox,
        ),
      );

      final action = await showMenu<_SettingsAction>(
        context: context,
        position: RelativeRect.fromRect(rect, Offset.zero & overlayBox.size),
        color: AppColors.primaryColor.withValues(alpha: 0.96),
        shape: RoundedRectangleBorder(
          borderRadius: AppBorders.xxxs,
          side: BorderSide(color: AppColors.darkGreen.withValues(alpha: 0.35)),
        ),
        items: const [
          PopupMenuItem(
            value: _SettingsAction.signOut,
            child: Row(
              children: [
                Icon(Icons.logout_outlined, size: 16, color: AppColors.white),
                SizedBox(width: 8),
                Text(
                  'Sign out',
                  style: TextStyle(color: AppColors.white, fontSize: 13),
                ),
              ],
            ),
          ),
          PopupMenuItem(
            value: _SettingsAction.deleteAccount,
            child: Row(
              children: [
                Icon(Icons.delete_outline, size: 16, color: AppColors.white),
                SizedBox(width: 8),
                Text(
                  'Delete account',
                  style: TextStyle(color: AppColors.white, fontSize: 13),
                ),
              ],
            ),
          ),
        ],
      );

      if (!context.mounted) return;
      if (action == _SettingsAction.signOut) {
        await context.read<SettingsCubit>().signOut();
      } else if (action == _SettingsAction.deleteAccount) {
        await context.read<DeleteAccountCubit>().requestAccountDeletion();
      }
    }

    return MultiBlocListener(
      listeners: [
        BlocListener<SettingsCubit, SettingsState>(
          listener: (context, state) {
            if (state is SettingsSuccess) {
              AuthState.setLoggedIn(false);
              AppRoutes.goLogin(context);
            } else if (state is SettingsError) {
              showSnackBar(context: context, message: state.message);
            }
          },
        ),
        BlocListener<DeleteAccountCubit, DeleteAccountState>(
          listener: (context, state) {
            if (state is DeleteAccountRequestSuccess) {
              AppRoutes.pushDeleteAccountVerification(context);
            } else if (state is DeleteAccountRequestError) {
              showSnackBar(
                context: context,
                message: state.message,
                backgroundColor: AppColors.red,
                textColor: AppColors.white,
              );
            }
          },
        ),
      ],
      child: SizedBox(
        height: 24,
        width: 24,
        child: PointerInterceptor(
          child: IconButton(
            tooltip: 'Settings',
            padding: EdgeInsets.zero,
            onPressed: openActionsDialog,
            icon: const Icon(
              Icons.settings_outlined,
              size: 18,
              color: AppColors.white,
            ),
          ),
        ),
      ),
    );
  }
}
