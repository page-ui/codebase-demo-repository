import 'package:flutter/material.dart';
import 'package:page_ui/core/helpers/custom_cli_loading_indicator.dart';

class HomeViewLoadingOverlay extends StatelessWidget {
  const HomeViewLoadingOverlay({super.key});

  @override
  Widget build(BuildContext context) {
    return const Positioned.fill(
      child: Stack(
        children: [
          ModalBarrier(dismissible: false, color: Colors.black38),
          Center(child: CustomCliLoadingIndicator()),
        ],
      ),
    );
  }
}
