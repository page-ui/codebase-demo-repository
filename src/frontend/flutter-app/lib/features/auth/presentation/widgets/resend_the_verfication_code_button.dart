import 'dart:async';

import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:flutter/material.dart';

class ResendTheVerficationCodeButton extends StatefulWidget {
  const ResendTheVerficationCodeButton({
    super.key,
    required this.onPressed,
    this.durationInSeconds = 120,
  });
  final void Function()? onPressed;
  final int durationInSeconds;
  @override
  State<ResendTheVerficationCodeButton> createState() =>
      _ResendTheVerficationCodeButtonState();
}

class _ResendTheVerficationCodeButtonState
    extends State<ResendTheVerficationCodeButton> {
  Timer? _timer;
  bool _isDisabled = true;

  @override
  void initState() {
    super.initState();
    _startTimer();
  }

  int _secondsRemaining = 0;

  void _startTimer() {
    _isDisabled = true;
    _secondsRemaining = widget.durationInSeconds;

    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_secondsRemaining == 0) {
        timer.cancel();
        setState(() {
          _isDisabled = false;
        });
      } else {
        setState(() {
          _secondsRemaining--;
        });
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  String get _formattedTime {
    final minutes = _secondsRemaining ~/ 60;
    final seconds = _secondsRemaining % 60;
    return "$minutes:${seconds.toString().padLeft(2, '0')}";
  }

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.topLeft,
      child: TextButton(
        onPressed: _isDisabled
            ? null
            : () {
                widget.onPressed?.call();
                _startTimer();
              },
        child: Text(
          _isDisabled ? "Resend in $_formattedTime" : "Resend the code",
          style: AppTextStyles.bodySmall!.copyWith(color: AppColors.white),
        ),
      ),
    );
  }
}
