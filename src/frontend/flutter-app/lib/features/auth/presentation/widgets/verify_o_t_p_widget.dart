import 'package:flutter/material.dart';
import 'package:page_ui/config/themes/app_colors.dart';

class VerifyOTPWidget extends StatefulWidget {
  const VerifyOTPWidget({super.key, required this.controllers});
  final List<TextEditingController> controllers;
  @override
  State<VerifyOTPWidget> createState() => _VerifyOTPWidgetState();
}

class _VerifyOTPWidgetState extends State<VerifyOTPWidget> {
  final List<FocusNode> _focusNodes = List.generate(5, (_) => FocusNode());

  void _onChanged(String value, int index) {
    if (index == 0 && value.length > 1) {
      final characters = value.split('');

      for (int i = 0; i < widget.controllers.length; i++) {
        if (i < characters.length) {
          widget.controllers[i].text = characters[i];
        } else {
          widget.controllers[i].clear();
        }
      }

      _focusNodes.last.requestFocus();
      return;
    }

    
    if (value.isNotEmpty && index < 4) {
      _focusNodes[index + 1].requestFocus();
    }

    if (value.isEmpty && index > 0) {
      _focusNodes[index - 1].requestFocus();
    }
  }

  @override
  void dispose() {
    for (final node in _focusNodes) {
      node.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: List.generate(5, (index) {
            return Flexible(
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 4),
                constraints: const BoxConstraints(maxWidth: 55, minWidth: 35),
                height: 60,
                decoration: BoxDecoration(
                  color: AppColors.black,
                  border: Border.all(color: AppColors.primaryColor, width: 1.5),
                  boxShadow: [
                    BoxShadow(
                      color: AppColors.primaryColor.withValues(alpha: 0.4),
                      blurRadius: 8,
                    ),
                  ],
                ),
                child: TextField(
                  controller: widget.controllers[index],
                  focusNode: _focusNodes[index],
                  textAlign: TextAlign.center,
                  maxLength: null,
                  style: const TextStyle(
                    color: AppColors.primaryColor,
                    fontSize: 22,
                  ),
                  cursorColor: AppColors.primaryColor,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                    counterText: "",
                    border: InputBorder.none,
                    contentPadding: EdgeInsets.zero,
                  ),
                  onChanged: (value) => _onChanged(value, index),
                ),
              ),
            );
          }),
        ),
      ],
    );
  }
}
