import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

@immutable
class SecureStorage {
  const SecureStorage._();
  static late final FlutterSecureStorage storage;

  static Future<FlutterSecureStorage> init() async =>
      storage = const FlutterSecureStorage();

  static Future<void> writeData({
    required String key,
    required String value,
  }) async {
    await storage.write(key: key, value: value);
  }

  static Future<String?> readData({required String key}) async {
    final token = await storage.read(key: key);
    return token;
  }

  static Future<void> deleteData({required String key}) async {
    await storage.delete(key: key);
  }

  static Future<void> deleteAllData() async {
    await storage.deleteAll();
  }

  static Future<bool> checkData({required String key}) async {
    final token = await storage.read(key: key);
    return token != null;
  }
}
