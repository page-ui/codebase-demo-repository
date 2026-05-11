import 'package:flutter/foundation.dart';
import 'package:page_ui/core/constants/constants.dart';
import 'package:page_ui/core/database/cache/secure_storage.dart';

@immutable
class AuthState {
  const AuthState._();

  static bool _isLoggedIn = false;

  static bool get isLoggedIn => _isLoggedIn;

  static Future<void> init() async {
    _isLoggedIn = await SecureStorage.checkData(key: tokensKey);
  }

  static void setLoggedIn(bool value) {
    _isLoggedIn = value;
  }
}
