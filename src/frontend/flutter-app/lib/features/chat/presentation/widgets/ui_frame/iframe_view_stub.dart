import 'package:flutter/material.dart';

class IframeView extends StatelessWidget {
  final String url;

  const IframeView({super.key, required this.url});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.black12,
      child: Center(
        child: Text(
          'Iframe not supported on this platform\nURL: $url',
          textAlign: TextAlign.center,
          style: const TextStyle(color: Colors.white70),
        ),
      ),
    );
  }
}
