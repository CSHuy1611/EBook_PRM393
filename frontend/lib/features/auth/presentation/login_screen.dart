import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/layout/responsive_layout.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/widgets/math_background.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  // GlobalKey dùng để quản lý trạng thái và kích hoạt validation của Form đăng nhập
  final _formKey = GlobalKey<FormState>();

  // Bộ điều khiển (Controller) dùng để lấy dữ liệu text từ các ô nhập liệu Email và Mật khẩu
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  // Trạng thái ẩn/hiện mật khẩu (true = ẩn dạng chấm tròn, false = hiện chữ thường)
  bool _obscurePassword = true;

  @override
  void dispose() {
    // Giải phóng bộ nhớ của các Controller khi không dùng màn hình này nữa để tránh rò rỉ bộ nhớ (memory leak)
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  // Hàm kiểm tra tính hợp lệ (validation) của định dạng Email nhập vào
  String? _validateEmail(String? value) {
    if (value == null || value.isEmpty) return 'Vui lòng nhập email';
    final emailRegex = RegExp(
      r'^[^@]+@[^@]+\.[^@]+$',
    ); // Biểu thức chính quy kiểm tra định dạng email
    if (!emailRegex.hasMatch(value)) return 'Email không hợp lệ';
    return null;
  }

  // Hàm xử lý khi người dùng nhấn nút "Đăng nhập"
  Future<void> _handleLogin() async {
    // 1. Kiểm tra tính hợp lệ của Form (nếu FormValidator báo lỗi thì dừng lại)
    if (!_formKey.currentState!.validate()) return;

    // 2. Lấy đối tượng AuthProvider thông qua read (không lắng nghe sự thay đổi liên tục, chỉ gọi hàm)
    final authProvider = context.read<AuthProvider>();
    try {
      // 3. Gọi hàm login để đóng gói dữ liệu và gửi HTTP POST request lên backend API
      await authProvider.login(
        _emailController.text.trim(),
        _passwordController.text,
      );
    } catch (e) {
      if (!mounted) return;
      // 4. Nếu có lỗi (sai pass, mất mạng...), chuyển đổi mã lỗi DioException thành thông điệp tiếng Việt thân thiện
      final message = e is DioException
          ? ApiClient.mapDioErrorToMessage(e)
          : e.toString();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(message), backgroundColor: Colors.red),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    // Watch AuthProvider để rebuild lại giao diện khi trạng thái đăng nhập thay đổi (ví dụ hiển thị nút Loading khi đang gọi API)
    final authProvider = context.watch<AuthProvider>();
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: MathBackground(
        child: ResponsiveAuthLayout(
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Logo
                Container(
                  width: 80,
                  height: 80,
                  decoration: BoxDecoration(
                    color: colorScheme.primary.withOpacity(0.12),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    Icons.menu_book_rounded,
                    size: 44,
                    color: colorScheme.primary,
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  'Math IBook',
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: colorScheme.primary,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Sách Toán 8 Tương Tác',
                  style: TextStyle(
                    fontSize: 14,
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 40),

                // Email - FULL WIDTH
                TextFormField(
                  controller: _emailController,
                  style: TextStyle(color: colorScheme.onSurface),
                  decoration: InputDecoration(
                    labelText: 'Email',
                    labelStyle: TextStyle(
                      color: colorScheme.onSurface.withOpacity(0.7),
                    ),
                    hintText: 'example@gmail.com',
                    prefixIcon: Icon(
                      Icons.email_outlined,
                      color: colorScheme.primary,
                    ),
                    filled: true,
                    fillColor: colorScheme.surfaceContainerHighest.withOpacity(
                      0.85,
                    ),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(
                        color: colorScheme.outline.withOpacity(0.5),
                      ),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(
                        color: colorScheme.outline.withOpacity(0.5),
                      ),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(
                        color: colorScheme.primary,
                        width: 2,
                      ),
                    ),
                  ),
                  keyboardType: TextInputType.emailAddress,
                  validator: _validateEmail,
                ),
                const SizedBox(height: 18),

                // Password - FULL WIDTH
                TextFormField(
                  controller: _passwordController,
                  style: TextStyle(color: colorScheme.onSurface),
                  decoration: InputDecoration(
                    labelText: 'Mật khẩu',
                    labelStyle: TextStyle(
                      color: colorScheme.onSurface.withOpacity(0.7),
                    ),
                    prefixIcon: Icon(
                      Icons.lock_outline,
                      color: colorScheme.primary,
                    ),
                    filled: true,
                    fillColor: colorScheme.surfaceContainerHighest.withOpacity(
                      0.85,
                    ),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(
                        color: colorScheme.outline.withOpacity(0.5),
                      ),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(
                        color: colorScheme.outline.withOpacity(0.5),
                      ),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(14),
                      borderSide: BorderSide(
                        color: colorScheme.primary,
                        width: 2,
                      ),
                    ),
                    suffixIcon: IconButton(
                      icon: Icon(
                        _obscurePassword
                            ? Icons.visibility_outlined
                            : Icons.visibility_off_outlined,
                        color: colorScheme.onSurface.withOpacity(0.6),
                      ),
                      onPressed: () =>
                          setState(() => _obscurePassword = !_obscurePassword),
                    ),
                  ),
                  obscureText: _obscurePassword,
                  validator: (value) {
                    if (value == null || value.isEmpty)
                      return 'Vui lòng nhập mật khẩu';
                    return null;
                  },
                ),
                const SizedBox(height: 10),
                Align(
                  alignment: Alignment.centerRight,
                  child: GestureDetector(
                    onTap: () => context.push('/forgot-password'),
                    child: MouseRegion(
                      cursor: SystemMouseCursors.click,
                      child: Text(
                        'Quên mật khẩu?',
                        style: TextStyle(
                          color: colorScheme.primary,
                          fontWeight: FontWeight.w600,
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 24),

                // Login button - FULL WIDTH
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: ElevatedButton(
                    onPressed: authProvider.isLoading ? null : _handleLogin,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: colorScheme.primary,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14),
                      ),
                      elevation: 2,
                    ),
                    child: authProvider.isLoading
                        ? const SizedBox(
                            height: 22,
                            width: 22,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.5,
                              valueColor: AlwaysStoppedAnimation<Color>(
                                Colors.white,
                              ),
                            ),
                          )
                        : const Text(
                            'Đăng nhập',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                  ),
                ),
                const SizedBox(height: 24),

                // Register link
                Wrap(
                  alignment: WrapAlignment.center,
                  children: [
                    Text(
                      'Chưa có tài khoản? ',
                      style: TextStyle(
                        color: colorScheme.onSurfaceVariant,
                        fontSize: 14,
                      ),
                    ),
                    GestureDetector(
                      onTap: () => context.go('/register'),
                      child: MouseRegion(
                        cursor: SystemMouseCursors.click,
                        child: Text(
                          'Đăng ký ngay',
                          style: TextStyle(
                            color: colorScheme.primary,
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
