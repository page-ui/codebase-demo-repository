import 'package:flutter/material.dart';

@immutable
sealed class AppColorsMean {
  const AppColorsMean();
  // Brand Colors
  static const Color brandPrimary = Color(0xff539062);
  static const Color brandAccent = Colors.greenAccent;
  static const Color brandSecondary = Color(0xFF00FF41);

  // Backgrounds
  static const Color appBackground = Color(0xff0f0f0f);
  static const Color scaffoldBackground = Colors.transparent;
  static const Color cardBackground = Color(0xff0f0f0f);
  static const Color surfaceBackground = Color.fromARGB(255, 61, 61, 61);
  static const Color darkBackground = Colors.black;
  static const Color sectionBackground = Color(0xFF2A2F33);

  // Text Colors
  static const Color mainText = Colors.white;
  static const Color subText = Color(0xFF9CA3AF);
  static const Color hintText = Color(0xFF9CA3AF);
  static const Color buttonTextLight = Colors.white;
  static const Color buttonTextDark = Colors.black;
  static const Color errorText = Colors.red;
  static const Color linkText = Color.fromARGB(255, 39, 220, 244);
  static const Color labelText = Color(0xFFE5E7EB);

  // Borders & Dividers
  static const Color primaryBorder = Color(0xff539062);
  static const Color secondaryBorder = Color(0xFF4B5563);
  static const Color lightBorder = Color.fromARGB(255, 231, 106, 106);
  static const Color focusedBorder = Colors.cyan;
  static const Color idleBorder = Color.fromARGB(255, 61, 61, 61);
  static const Color dividerColor = Color(0xFFE5E7EB);
  static const Color activeBorder = Color.fromARGB(255, 252, 206, 69);

  // Feedback & Status
  static const Color success = Colors.green;
  static const Color error = Colors.red;
  static const Color warning = Colors.amber;
  static const Color info = Colors.cyan;

  // Overlays & Special
  static const Color overlayBackground = Colors.black;
  static const Color shadowColor = Colors.black;
  static const Color transparent = Colors.transparent;

  // Component Specific
  static const Color scrollbarHandle = Color(0xff539062);
  static const Color scrollbarTrack = Color(0xFFE5E7EB);
  static const Color loadingDotPrimary = Colors.amber;
  static const Color loadingDotSecondary = Colors.greenAccent;
  static const Color snackBarBackground = Colors.black;
  static const Color snackBarErrorBackground = Colors.red;
}
