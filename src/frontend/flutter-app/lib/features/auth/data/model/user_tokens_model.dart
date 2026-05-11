import 'dart:convert';

import 'package:page_ui/core/constants/constants.dart';
import 'package:page_ui/core/database/cache/secure_storage.dart';

class UserTokensModel {
  final String? accessToken;
  final String? refreshToken;

  UserTokensModel({required this.accessToken, required this.refreshToken});

  factory UserTokensModel.fromJson(Map<String, dynamic> json) {
    return UserTokensModel(
      accessToken: json['accessToken'],
      refreshToken: json['refreshToken'],
    );
  }
  Map<String, dynamic> toJson() {
    return {'accessToken': accessToken, 'refreshToken': refreshToken};
  }

  bool isEmpty() {
    return accessToken == null || refreshToken == null;
  }
}

Future<UserTokensModel> returnTokensFromSecureDB() async {
  String tokens = await SecureStorage.readData(key: tokensKey) ?? "";

  UserTokensModel userTokensModel = UserTokensModel(
    accessToken: null,
    refreshToken: null,
  );
  if (!tokens.isEmpty) {
    Map<String, dynamic> tokensDecoder = const JsonDecoder().convert(tokens);
    userTokensModel = UserTokensModel.fromJson(tokensDecoder);
  }
  return userTokensModel;
}

Future<void> saveTokens(UserTokensModel userTokensModel) async {
  var tokens = const JsonEncoder().convert(userTokensModel.toJson());
  await SecureStorage.writeData(key: tokensKey, value: tokens);
}
