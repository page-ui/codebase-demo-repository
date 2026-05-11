import 'package:flutter/material.dart';
import 'package:page_ui/features/auth/presentation/widgets/OTP_code_verfication.dart';
import 'package:page_ui/features/auth/presentation/widgets/forget_password_request.dart';
import 'package:page_ui/features/auth/presentation/widgets/password_reset.dart';

class ForgetPaswordViewForm extends StatefulWidget {
  const ForgetPaswordViewForm({super.key});

  @override
  State<ForgetPaswordViewForm> createState() => _ForgetPaswordViewFormState();
}

class _ForgetPaswordViewFormState extends State<ForgetPaswordViewForm> {
  late List<TextEditingController> controllers;
  AutovalidateMode autovalidateMode = AutovalidateMode.disabled;
  String? email;
  String? token;

  final ScrollController _scrollController1 = ScrollController();
  final ScrollController _scrollController2 = ScrollController();
  final ScrollController _scrollController3 = ScrollController();

  @override
  void initState() {
    super.initState();
    controllers = List.generate(5, (_) => TextEditingController());
  }

  @override
  void dispose() {
    controllers.forEach((controller) => controller.dispose());
    _scrollController1.dispose();
    _scrollController2.dispose();
    _scrollController3.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (_currentStep > 0)
          Align(
            alignment: Alignment.centerLeft,
            child: IconButton(
              icon: const Icon(Icons.arrow_back),
              onPressed: previousStep,
            ),
          ),
        Container(
          constraints: const BoxConstraints(minHeight: 0, maxHeight: 300),
          child: PageView(
            controller: _controller,
            physics: const NeverScrollableScrollPhysics(),
            children: [
              Scrollbar(
                controller: _scrollController1,
                thumbVisibility: true,
                child: SingleChildScrollView(
                  controller: _scrollController1,
                  child: ForgetPasswordRequest(
                    nextStep: nextStep,
                    onEmailChanged: (String e) {
                      setState(() {
                        email = e;
                      });
                    },
                  ),
                ),
              ),
              Scrollbar(
                controller: _scrollController2,
                thumbVisibility: true,
                child: SingleChildScrollView(
                  controller: _scrollController2,
                  child: OTPCodeVerfication(
                    controllers: controllers,
                    nextStep: nextStep,
                    email: email ?? "",
                    onGetToken: (String value) {
                      setState(() {
                        token = value;
                      });
                    },
                  ),
                ),
              ),
              Scrollbar(
                controller: _scrollController3,
                thumbVisibility: true,
                child: SingleChildScrollView(
                  controller: _scrollController3,
                  child: PasswordReset(email: email ?? "", token: token ?? ""),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  final PageController _controller = PageController();
  int _currentStep = 0;

  void nextStep() {
    for (final controller in controllers) {
      controller.clear();
    }
    _controller.nextPage(
      duration: const Duration(milliseconds: 400),
      curve: Curves.easeOutCubic,
    );
    setState(() => _currentStep++);
  }

  void previousStep() {
    for (final controller in controllers) {
      controller.clear();
    }
    _controller.previousPage(
      duration: const Duration(milliseconds: 400),
      curve: Curves.easeOutCubic,
    );

    setState(() => _currentStep--);
  }
}
