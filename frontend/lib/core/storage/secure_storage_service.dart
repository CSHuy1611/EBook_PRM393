import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/user_model.dart';

class SecureStorageService {
  final FlutterSecureStorage _storage;

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _userDataKey = 'user_data';

  SecureStorageService()
      : _storage = const FlutterSecureStorage(
          aOptions: AndroidOptions(encryptedSharedPreferences: false),
        );

  Future<void> saveAccessToken(String token) async {
    await _storage.write(key: _accessTokenKey, value: token);
  }

  Future<String?> getAccessToken() async {
    return await _storage.read(key: _accessTokenKey);
  }

  Future<void> saveRefreshToken(String token) async {
    await _storage.write(key: _refreshTokenKey, value: token);
  }

  Future<String?> getRefreshToken() async {
    return await _storage.read(key: _refreshTokenKey);
  }

  Future<void> saveUser(UserModel user) async {
    await _storage.write(
      key: _userDataKey,
      value: jsonEncode(user.toJson()),
    );
  }

  Future<UserModel?> getUser() async {
    final data = await _storage.read(key: _userDataKey);
    if (data == null) return null;
    return UserModel.fromJson(jsonDecode(data));
  }

  Future<void> saveToken(String token) => saveAccessToken(token);
  Future<String?> getToken() => getAccessToken();

  Future<void> clearAll() async {
    await _storage.deleteAll();
  }
}
