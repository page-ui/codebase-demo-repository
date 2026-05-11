import 'dart:async';

import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/enum/screen_type.dart';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

class CustomCliLoadingIndicator extends StatefulWidget {
  const CustomCliLoadingIndicator({
    super.key,
    this.accentColor = AppColors.darkGreen,
  });

  final Color accentColor;

  @override
  State<CustomCliLoadingIndicator> createState() =>
      _CustomCliLoadingIndicatorState();
}

class _CustomCliLoadingIndicatorState extends State<CustomCliLoadingIndicator> {
  late Timer _timer;
  int _progress = 0;
  bool _showCursor = true;
  final List<String> _logs = [];

  final List<String> _stages = [
    "> Establishing connection...",
    "> Handshake verified.",
    "> Encrypting payload...",
    "> Sending data packets...",
    "> Awaiting server ACK...",
  ];

  @override
  void initState() {
    super.initState();

    _timer = Timer.periodic(const Duration(milliseconds: 150), (timer) {
      setState(() {
        if (timer.tick % 3 == 0) _showCursor = !_showCursor;
        _progress = (_progress + 2) % 102;
        if (_progress == 0) {
          _logs.clear();
        }
        int stageIndex = (_progress / 20).floor();
        if (stageIndex < _stages.length &&
            !_logs.contains(_stages[stageIndex])) {
          _logs.add(_stages[stageIndex]);
        }
      });
    });
  }

  @override
  void dispose() {
    _timer.cancel();
    super.dispose();
  }

  String get progressBar {
    const int totalBlocks = 15;
    int filled = ((_progress.clamp(0, 100) / 100) * totalBlocks).floor();
    return '[${'#' * filled}${' ' * (totalBlocks - filled)}]';
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      width: context.isMobile ? 1000.w : 650.w,
      decoration: BoxDecoration(
        color: AppColors.black,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.white10, width: 1),
        boxShadow: [
          BoxShadow(
            color: AppColors.black.withValues(alpha: 0.5),
            blurRadius: 20,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildTerminalHeader(),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8.0, vertical: 16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.start,
              children: [
                ..._logs.map(
                  (log) => Padding(
                    padding: const EdgeInsets.only(bottom: 4),
                    child: Text(log, style: _terminalStyle()),
                  ),
                ),
                const SizedBox(height: 8),
                Wrap(
                  children: [
                    Text(progressBar, style: _terminalStyle()),
                    const SizedBox(width: 10),
                    Text(
                      '${_progress.clamp(0, 100)}%',
                      style: _terminalStyle(bold: true),
                    ),
                    if (_showCursor)
                      Text(
                        ' █',
                        style: TextStyle(
                          color: widget.accentColor,
                          fontSize: 12,
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTerminalHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: const BoxDecoration(
        color: Color(0xFF1E1E1E),
        borderRadius: BorderRadius.vertical(top: Radius.circular(8)),
      ),
      child: Row(
        children: [
          const Expanded(
            child: Text(
              "bash — service_monitor.sh",
              textAlign: TextAlign.center,
              style: TextStyle(
                color: Colors.white38,
                fontSize: 11,
                fontFamily: 'monospace',
              ),
              softWrap: true,
              overflow: TextOverflow.ellipsis,
            ),
          ),

          _dot(AppColors.amber),
          const SizedBox(width: 6),
          _dot(AppColors.greenAccent),
          const SizedBox(width: 6),
          _dot(Colors.redAccent),
        ],
      ),
    );
  }

  Widget _dot(Color color) => Container(
    width: 8,
    height: 8,
    decoration: BoxDecoration(color: color, shape: BoxShape.circle),
  );

  TextStyle _terminalStyle({bool bold = false}) {
    return TextStyle(
      color: widget.accentColor,
      fontFamily: 'monospace',
      fontSize: 13,
      fontWeight: bold ? FontWeight.bold : FontWeight.normal,
    );
  }
}
