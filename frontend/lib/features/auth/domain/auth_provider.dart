import 'package:flutter/foundation.dart';
import 'package:math_ibook/core/models/auth_response.dart';
import 'package:math_ibook/core/models/user_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/secure_storage_service.dart';

class AuthProvider extends ChangeNotifier {
  final ApiClient _apiClient;
  final SecureStorageService _secureStorage;

  UserModel? _currentUser;
  bool _isLoading = false;
  bool _isAuthenticated = false;

  AuthProvider(this._secureStorage)
      : _apiClient = ApiClient.instance;

  UserModel? get currentUser => _currentUser;
  bool get isLoading => _isLoading;
  bool get isAuthenticated => _isAuthenticated;

  void _setLoading(bool value) {
    _isLoading = value;
    notifyListeners();
  }

  Future<void> tryAutoLogin() async {
    _setLoading(true);
    try {
      final savedUser = await _secureStorage.getUser();
      final savedToken = await _secureStorage.getAccessToken();
      if (savedUser != null && savedToken != null && savedToken.isNotEmpty) {
        _currentUser = savedUser;
        _isAuthenticated = true;
      }
    } catch (_) {
    } finally {
      _setLoading(false);
    }
  }

  Future<AuthResponse> login(String email, String password) async {
    _setLoading(true);
    try {
      final data = await _apiClient.login(email, password);
      final response = AuthResponse.fromJson(data);
      await _secureStorage.saveAccessToken(response.token);
      await _secureStorage.saveRefreshToken(response.refreshToken);
      await _secureStorage.saveUser(response.user);
      _currentUser = response.user;
      _isAuthenticated = true;
      return response;
    } finally {
      _setLoading(false);
    }
  }

  Future<AuthResponse> register(
    String name,
    String email,
    String password,
    String confirmPassword,
  ) async {
    _setLoading(true);
    try {
      final data = await _apiClient.register(name, email, password, confirmPassword);
      final response = AuthResponse.fromJson(data);
      await _secureStorage.saveAccessToken(response.token);
      await _secureStorage.saveRefreshToken(response.refreshToken);
      await _secureStorage.saveUser(response.user);
      _currentUser = response.user;
      _isAuthenticated = true;
      return response;
    } finally {
      _setLoading(false);
    }
  }

  Future<void> logout() async {
    _setLoading(true);
    try {
      await _apiClient.logout();
    } catch (_) {
    } finally {
      await _secureStorage.clearAll();
      _currentUser = null;
      _isAuthenticated = false;
      _setLoading(false);
    }
  }

  Future<void> refreshToken() async {
    try {
      final refreshToken = await _secureStorage.getRefreshToken();
      if (refreshToken != null && refreshToken.isNotEmpty) {
        final data = await _apiClient.refreshToken(refreshToken);
        final response = AuthResponse.fromJson(data);
        await _secureStorage.saveAccessToken(response.token);
        await _secureStorage.saveRefreshToken(response.refreshToken);
      }
    } catch (_) {
      await logout();
    }
  }

  void addCoins(int amount) {
    if (amount <= 0 || _currentUser == null) return;
    _currentUser = _currentUser!.copyWith(coins: _currentUser!.coins + amount);
    _secureStorage.saveUser(_currentUser!);
    notifyListeners();
  }
}
