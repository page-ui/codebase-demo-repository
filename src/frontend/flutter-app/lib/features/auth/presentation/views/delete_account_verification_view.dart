import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:page_ui/core/custom_widget/custom_button.dart';
import 'package:page_ui/core/helpers/auth_state.dart';
import 'package:page_ui/core/helpers/custom_show_snack_bar.dart';
import 'package:page_ui/core/helpers/setup_service_locator_getit.dart';
import 'package:page_ui/features/auth/data/repos/auth_repo_impl.dart';
import 'package:page_ui/features/auth/presentation/controllers/delete_account_cubit/delete_account_cubit.dart';
import 'package:page_ui/features/auth/presentation/widgets/custom_auth_screen_theme.dart';
import 'package:page_ui/features/auth/presentation/widgets/resend_the_verfication_code_button.dart';
import 'package:page_ui/features/auth/presentation/widgets/verify_o_t_p_widget.dart';

class DeleteAccountVerificationView extends StatelessWidget {
  const DeleteAccountVerificationView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => DeleteAccountCubit(getit.get<AuthRepoImpl>()),
      child: Scaffold(
        appBar: AppBar(
          backgroundColor: Colors.transparent,
          elevation: 0,
          leading: IconButton(
            icon: const Icon(Icons.arrow_back),
            onPressed: () {
              Navigator.of(context).pop();
            },
          ),
        ),
        body: const CustomAuthScreenTheme(
          viewTitle: 'DELETE ACCOUNT',
          child: DeleteAccountOTPWidget(),
        ),
      ),
    );
  }
}

class DeleteAccountOTPWidget extends StatefulWidget {
  const DeleteAccountOTPWidget({super.key});

  @override
  State<DeleteAccountOTPWidget> createState() => _DeleteAccountOTPWidgetState();
}

class _DeleteAccountOTPWidgetState extends State<DeleteAccountOTPWidget> {
  final List<TextEditingController> controllers =
      List.generate(5, (_) => TextEditingController());
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;
  final GlobalKey<FormState> formKey = GlobalKey<FormState>();
  bool isLoading = false;

  @override
  void dispose() {
    for (var controller in controllers) {
      controller.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    const text = Text(
      "VERIFY ACCOUNT DELETION",
      style: TextStyle(
        color: AppColors.primaryColor,
        fontSize: 22,
        letterSpacing: 1.5,
        overflow: TextOverflow.clip,
      ),
    );

    return BlocListener<DeleteAccountCubit, DeleteAccountState>(
      listener: (context, state) {
        if (state is DeleteAccountVerifySuccess) {
          setState(() {
            isLoading = false;
          });
          showSnackBar(context: context, message: 'Account Deleted Successfully.');
          AuthState.setLoggedIn(false);
          AppRoutes.goLogin(context);
        } else if (state is DeleteAccountVerifyError) {
          setState(() {
            isLoading = false;
          });
          showSnackBar(
            context: context,
            message: state.message,
            backgroundColor: AppColors.red,
            textColor: AppColors.white,
          );
        } else if (state is DeleteAccountVerifyLoading) {
          setState(() {
            isLoading = true;
          });
        } else if (state is DeleteAccountRequestError) {
          showSnackBar(
            context: context,
            message: state.message,
            backgroundColor: AppColors.red,
            textColor: AppColors.white,
          );
        } else if (state is DeleteAccountRequestSuccess) {
          showSnackBar(
            context: context,
            message: 'Verification code resent successfully.',
          );
        }
      },
      child: Form(
        key: formKey,
        autovalidateMode: autovalidateMode,
        child: Column(
          children: [
            text,
            const SizedBox(height: 8),
            const Text(
              "ENTER THE 5-DIGIT CODE SENT TO YOUR EMAIL",
              style: TextStyle(color: AppColors.primaryColor, fontSize: 13),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            VerifyOTPWidget(controllers: controllers),
            const SizedBox(height: 30),
            ResendTheVerficationCodeButton(
              durationInSeconds: 180,
              onPressed: () {
                context.read<DeleteAccountCubit>().requestAccountDeletion();
              },
            ),
            const SizedBox(height: 8),
            CustomButton(
              title: isLoading ? "DELETING..." : "DELETE ACCOUNT",
              onPressed: isLoading
                  ? null
                  : () {
                      for (var controller in controllers) {
                        if (controller.text.isEmpty) {
                          showSnackBar(
                            context: context,
                            message: 'OTP not completed.',
                            backgroundColor: AppColors.red,
                            textColor: AppColors.white,
                          );
                          return;
                        }
                      }
                      if (formKey.currentState!.validate()) {
                        FocusScope.of(context).unfocus();
                        final code = controllers.map((e) => e.text).join();
                        context.read<DeleteAccountCubit>().verifyDeletion(code);
                      } else {
                        setState(() {
                          autovalidateMode = AutovalidateMode.always;
                        });
                      }
                    },
            ),
            const SizedBox(height: 8),
            Align(
              alignment: AlignmentGeometry.bottomLeft,
              child: Text(
                "Note: email maybe in spam emails.",
                style: AppTextStyles.bodyMedium!.copyWith(color: AppColors.red),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
