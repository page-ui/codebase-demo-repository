import 'package:flutter/material.dart';

@immutable
sealed class AppColors {
  const AppColors();
  static const Color primaryColor = Color(0xff539062);
  static const Color mainBackgroundColor = Color(0xff0f0f0f);
  static const Color lightAmber = Color.fromARGB(255, 252, 206, 69);
  static const Color greenAccent = Colors.greenAccent;
  
  static const Color darkGreen = Color(0xFF00FF41);
  static const Color amber = Colors.amber;
  static const Color black = Colors.black;
  static const Color cyan = Colors.cyan;
  static const Color lightCyan = Color.fromARGB(255, 39, 220, 244);

  static const Color darkGrey = Color(0xFF4B5563);
  static const Color grey = Colors.grey;
  static const Color green = Colors.green;
  static const Color red = Colors.red;
  static const Color transparent = Colors.transparent;
  static const Color white = Colors.white;

  static const Color lightRed = Color.fromARGB(255, 231, 106, 106);
  static const Color lightGray = Color(0xFFE5E7EB);
  static const Color textGray = Color(0xFF9CA3AF);
  static const Color darkSurface = Color.fromARGB(255, 61, 61, 61);
  static const Color anotherGray = Color(0xFF2A2F33);
}
