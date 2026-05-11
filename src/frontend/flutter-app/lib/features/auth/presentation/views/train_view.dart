import 'dart:async';

import 'package:page_ui/config/routes/on_generate_routes.dart';
import 'package:page_ui/features/chat/presentation/widgets/ui_frame/iframe_view.dart';
import 'package:flutter/material.dart';

class TrainView extends StatefulWidget {
  const TrainView({super.key});
  static const String routeName = "TrainView";

  static const String _slUrl = '/train/index.html';

  @override
  State<TrainView> createState() => _TrainViewState();
}

class _TrainViewState extends State<TrainView> {
  Timer? _timer;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_timer != null) return;
    final width = MediaQuery.of(context).size.width;
    final int seconds;
    if (width < 600) {
      seconds = 7;
    } else if (width < 1024) {
      seconds = 9;
    } else {
      seconds = 12;
    }
    _timer = Timer(Duration(seconds: seconds), () {
      if (!mounted) return;
      AppRoutes.goHome(context);
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Colors.black,
      body: IframeView(url: TrainView._slUrl),
    );
  }
}
