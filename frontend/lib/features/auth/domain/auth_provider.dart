import 'package:flutter/foundation.dart';
import 'package:math_ibook/core/models/auth_response.dart';
import 'package:math_ibook/core/models/user_model.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/secure_storage_service.dart';

// AuthProvider kế thừa ChangeNotifier để quản lý trạng thái đăng nhập toàn ứng dụng
// Khi trạng thái thay đổi (đăng nhập thành công, đăng xuất, cộng xu), nó gọi notifyListeners() để rebuild các giao diện đang quan sát.
class AuthProvider extends ChangeNotifier {
  final ApiClient _apiClient; // Dùng để gọi các API HTTP request lên server
  final SecureStorageService _secureStorage; // Dùng để lưu trữ Token và User bảo mật xuống ổ cứng điện thoại

  UserModel? _currentUser; // Thông tin tài khoản người dùng hiện tại đang đăng nhập
  bool _isLoading = false; // Trạng thái đang gọi API (loading)
  bool _isAuthenticated = false; // Trạng thái xác thực (true: đã đăng nhập, false: chưa đăng nhập)

  AuthProvider(this._secureStorage)
      : _apiClient = ApiClient.instance;

  UserModel? get currentUser => _currentUser;
  bool get isLoading => _isLoading;
  bool get isAuthenticated => _isAuthenticated;

  // Cập nhật trạng thái loading và thông báo cho UI vẽ lại (ví dụ hiện vòng xoay hoặc khóa nút bấm)
  void _setLoading(bool value) {
    _isLoading = value;
    notifyListeners();
  }

  // Tự động đăng nhập: Khi vừa mở app, đọc thông tin User và Token đã lưu từ phiên trước
  Future<void> tryAutoLogin() async {
    _setLoading(true);
    try {
      final savedUser = await _secureStorage.getUser();
      final savedToken = await _secureStorage.getAccessToken();
      // Nếu có đầy đủ User và Token hợp lệ -> Chuyển trạng thái đăng nhập thành công
      if (savedUser != null && savedToken != null && savedToken.isNotEmpty) {
        _currentUser = savedUser;
        _isAuthenticated = true;
      }
    } catch (_) {
    } finally {
      _setLoading(false);
    }
  }

  // Đăng nhập: Gửi email/mật khẩu lên server, nhận token và lưu lại
  Future<AuthResponse> login(String email, String password) async {
    _setLoading(true);
    try {
      final data = await _apiClient.login(email, password); // Gọi API đăng nhập ở backend
      final response = AuthResponse.fromJson(data);
      
      // Lưu trữ thông tin đăng nhập bảo mật xuống máy điện thoại
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

  // Đăng ký tài khoản: Gửi thông tin đăng ký kèm mã xác thực OTP lên server
  Future<AuthResponse> register(
    String name,
    String email,
    String password,
    String confirmPassword,
    String otp,
  ) async {
    _setLoading(true);
    try {
      final data = await _apiClient.register(name, email, password, confirmPassword, otp);
      final response = AuthResponse.fromJson(data);
      
      // Đăng ký thành công -> Lưu Token và tự động chuyển sang trạng thái đã đăng nhập
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

  // Yêu cầu gửi mã OTP kích hoạt đăng ký tài khoản tới Email
  Future<void> requestOtp(String email) async {
    _setLoading(true);
    try {
      await _apiClient.sendOtp(email);
    } finally {
      _setLoading(false);
    }
  }

  // Yêu cầu gửi mã OTP đặt lại mật khẩu (quên mật khẩu) tới Email
  Future<void> forgotPassword(String email) async {
    _setLoading(true);
    try {
      await _apiClient.forgotPassword(email);
    } finally {
      _setLoading(false);
    }
  }

  // Đặt lại mật khẩu mới: Xác thực bằng mã OTP và cập nhật mật khẩu mới lên server
  Future<void> resetPassword(String email, String otp, String newPassword, String confirmNewPassword) async {
    _setLoading(true);
    try {
      await _apiClient.resetPassword(email, otp, newPassword, confirmNewPassword);
    } finally {
      _setLoading(false);
    }
  }

  // Đăng xuất: Xóa sạch token, thông tin user lưu trong máy và đưa app về trạng thái chưa đăng nhập
  Future<void> logout() async {
    _setLoading(true);
    try {
      await _apiClient.logout();
    } catch (_) {
    } finally {
      await _secureStorage.clearAll(); // Xóa sạch dữ liệu trong Secure Storage
      _currentUser = null;
      _isAuthenticated = false;
      _setLoading(false);
    }
  }

  // Làm mới Token: Tự động chạy ngầm khi Token cũ hết hạn để lấy Token mới bằng Refresh Token
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
      await logout(); // Nếu lỗi làm mới token (Refresh Token hết hạn) -> Đăng xuất người dùng ngay
    }
  }

  // Cộng xu học tập: Cộng thêm xu khi hoàn thành bài tập và lưu lại vào SecureStorage dưới máy
  void addCoins(int amount) {
    if (amount <= 0 || _currentUser == null) return;
    _currentUser = _currentUser!.copyWith(coins: _currentUser!.coins + amount);
    _secureStorage.saveUser(_currentUser!);
    notifyListeners(); // Thông báo giao diện vẽ lại số xu mới
  }

  // Cập nhật thông tin User (ví dụ khi đổi Avatar hoặc Tên)
  void updateUser(UserModel updatedUser) {
    _currentUser = updatedUser;
    _secureStorage.saveUser(_currentUser!);
    notifyListeners();
  }
}
