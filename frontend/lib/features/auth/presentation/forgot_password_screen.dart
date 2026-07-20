import 'dart:async';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/layout/responsive_layout.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/widgets/math_background.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  // GlobalKey quản lý validation cho từng bước (1: Email, 2: OTP, 3: Mật khẩu mới)
  final _emailFormKey = GlobalKey<FormState>();
  final _otpFormKey = GlobalKey<FormState>();
  final _passwordFormKey = GlobalKey<FormState>();

  // Các Controller lưu trữ nội dung nhập vào cho từng ô dữ liệu
  final _emailController = TextEditingController();
  final _otpController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  // Biến đếm bước hiện tại (1: Nhập email, 2: Nhập OTP xác thực, 3: Đặt mật khẩu mới, 4: Thành công)
  int _currentStep = 1;
  bool _obscurePassword = true; // Ẩn/hiện mật khẩu mới
  bool _obscureConfirm = true; // Ẩn/hiện xác nhận mật khẩu
  bool _isSendingRequest = false; // Trạng thái đang tải dữ liệu từ API
  int _otpCountdown = 0; // Thời gian đếm ngược (giây) để gửi lại OTP
  Timer? _timer; // Đối tượng Timer đếm ngược

  @override
  void dispose() {
    // Hủy bộ hẹn giờ và giải phóng các Controller khi đóng màn hình
    _timer?.cancel();
    _emailController.dispose();
    _otpController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  // Hàm đếm ngược 30 giây để gửi lại OTP xác thực
  void _startCountdown() {
    setState(() {
      _otpCountdown = 30;
    });
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_otpCountdown == 0) {
        timer.cancel();
      } else {
        setState(() {
          _otpCountdown--;
        });
      }
    });
  }

  // Hàm validate email
  String? _validateEmail(String? value) {
    if (value == null || value.isEmpty) return 'Vui lòng nhập email';
    if (!RegExp(r'^[^@]+@[^@]+\.[^@]+$').hasMatch(value))
      return 'Email không hợp lệ';
    return null;
  }

  // Hàm validate mật khẩu mới (tối thiểu 6 ký tự, 1 chữ hoa, 1 số)
  String? _validatePassword(String? value) {
    if (value == null || value.isEmpty) return 'Vui lòng nhập mật khẩu mới';
    if (value.length < 6) return 'Mật khẩu tối thiểu 6 ký tự';
    if (!value.contains(RegExp(r'[A-Z]'))) return 'Cần ít nhất 1 chữ hoa';
    if (!value.contains(RegExp(r'[0-9]'))) return 'Cần ít nhất 1 chữ số';
    return null;
  }

  // BƯỚC 1: Nhấn gửi -> Gọi API gửi mã OTP quên mật khẩu vào Email
  Future<void> _sendOtp() async {
    if (!_emailFormKey.currentState!.validate()) return;
    setState(() {
      _isSendingRequest = true;
    });

    try {
      // Gọi API gửi mã OTP khôi phục mật khẩu tới email
      await context.read<AuthProvider>().forgotPassword(
        _emailController.text.trim(),
      );
      _startCountdown();
      setState(() {
        _currentStep = 2; // Chuyển sang bước 2 (nhập OTP)
        _isSendingRequest = false;
      });
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Mã OTP xác thực đã được gửi đến Email của bạn.'),
          backgroundColor: Colors.green,
        ),
      );
    } catch (e) {
      setState(() {
        _isSendingRequest = false;
      });
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            e is DioException
                ? ApiClient.mapDioErrorToMessage(e)
                : e.toString(),
          ),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  // BƯỚC 2: Kiểm tra sơ bộ định dạng mã OTP và chuyển sang màn đặt Mật khẩu mới
  void _verifyOtpAndGoToPassword() {
    if (!_otpFormKey.currentState!.validate()) return;
    setState(() {
      _currentStep = 3; // Chuyển sang bước 3 (đặt mật khẩu mới)
    });
  }

  // BƯỚC 3: Gửi Mật khẩu mới kèm OTP và Email lên Server để cập nhật lại mật khẩu mới
  Future<void> _handleResetPassword() async {
    if (!_passwordFormKey.currentState!.validate()) return;
    setState(() {
      _isSendingRequest = true;
    });

    try {
      // Gọi API reset password
      await context.read<AuthProvider>().resetPassword(
        _emailController.text.trim(),
        _otpController.text.trim(),
        _passwordController.text,
        _confirmPasswordController.text,
      );

      setState(() {
        _currentStep = 4; // Chuyển sang màn hình báo thành công
        _isSendingRequest = false;
      });

      // Tự động chuyển hướng về màn hình đăng nhập sau 2 giây
      Timer(const Duration(seconds: 2), () {
        if (mounted) {
          context.go('/login');
        }
      });
    } catch (e) {
      setState(() {
        _isSendingRequest = false;
      });
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            e is DioException
                ? ApiClient.mapDioErrorToMessage(e)
                : e.toString(),
          ),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  InputDecoration _inputDeco(
    String label,
    IconData icon, {
    Widget? suffix,
    String? hint,
  }) {
    final colorScheme = Theme.of(context).colorScheme;
    return InputDecoration(
      labelText: label,
      hintText: hint,
      prefixIcon: Icon(icon),
      suffixIcon: suffix,
      filled: true,
      fillColor: Colors.white.withOpacity(0.9),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: Colors.grey.shade300),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: Colors.grey.shade300),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide(color: colorScheme.primary, width: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: MathBackground(
        child: ResponsiveAuthLayout(
          child: AnimatedSwitcher(
            duration: const Duration(milliseconds: 300),
            child: _buildCurrentStepWidget(colorScheme),
          ),
        ),
      ),
    );
  }

  Widget _buildCurrentStepWidget(ColorScheme colorScheme) {
    switch (_currentStep) {
      case 1:
        return _buildEmailStep(colorScheme);
      case 2:
        return _buildOtpStep(colorScheme);
      case 3:
        return _buildPasswordStep(colorScheme);
      case 4:
        return _buildSuccessStep(colorScheme);
      default:
        return _buildEmailStep(colorScheme);
    }
  }

  // ─── BƯỚC 1: NHẬP EMAIL ───────────────────────────────────
  Widget _buildEmailStep(ColorScheme colorScheme) {
    return Form(
      key: _emailFormKey,
      child: Column(
        key: const ValueKey('email_step'),
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(
              color: colorScheme.primary.withOpacity(0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(
              Icons.lock_reset_rounded,
              size: 44,
              color: colorScheme.primary,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Quên Mật Khẩu?',
            style: TextStyle(
              fontSize: 26,
              fontWeight: FontWeight.bold,
              color: colorScheme.primary,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'Nhập email của bạn để nhận mã OTP xác thực đặt lại mật khẩu',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 14,
              color: Colors.grey.shade600,
              height: 1.4,
            ),
          ),
          const SizedBox(height: 32),

          TextFormField(
            controller: _emailController,
            decoration: _inputDeco(
              'Email tài khoản',
              Icons.email_outlined,
              hint: 'example@gmail.com',
            ),
            keyboardType: TextInputType.emailAddress,
            validator: _validateEmail,
          ),
          const SizedBox(height: 32),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: _isSendingRequest ? null : _sendOtp,
              style: ElevatedButton.styleFrom(
                backgroundColor: colorScheme.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
                elevation: 2,
              ),
              child: _isSendingRequest
                  ? const SizedBox(
                      height: 22,
                      width: 22,
                      child: CircularProgressIndicator(
                        strokeWidth: 2.5,
                        valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                      ),
                    )
                  : const Text(
                      'Gửi mã OTP',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
            ),
          ),
          const SizedBox(height: 24),

          TextButton.icon(
            onPressed: () => context.go('/login'),
            icon: const Icon(Icons.arrow_back, size: 16),
            label: const Text('Quay lại đăng nhập'),
          ),
        ],
      ),
    );
  }

  // ─── BƯỚC 2: NHẬP OTP ─────────────────────────────────────
  Widget _buildOtpStep(ColorScheme colorScheme) {
    return Form(
      key: _otpFormKey,
      child: Column(
        key: const ValueKey('otp_step'),
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(
              color: Colors.orange.withOpacity(0.12),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.security_rounded,
              size: 44,
              color: Colors.orange,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Nhập Mã OTP',
            style: TextStyle(
              fontSize: 26,
              fontWeight: FontWeight.bold,
              color: colorScheme.primary,
            ),
          ),
          const SizedBox(height: 8),
          RichText(
            textAlign: TextAlign.center,
            text: TextSpan(
              style: TextStyle(
                fontSize: 14,
                color: Colors.grey.shade600,
                height: 1.4,
              ),
              children: [
                const TextSpan(text: 'Mã xác thực đã được gửi tới email\n'),
                TextSpan(
                  text: _emailController.text.trim(),
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: colorScheme.primary,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),

          TextFormField(
            controller: _otpController,
            decoration: _inputDeco('Mã OTP', Icons.vpn_key_outlined),
            keyboardType: TextInputType.number,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.bold,
              letterSpacing: 10,
            ),
            validator: (v) {
              if (v == null || v.isEmpty) return 'Vui lòng nhập mã OTP';
              if (v.length != 6) return 'Mã OTP gồm đúng 6 chữ số';
              return null;
            },
          ),
          const SizedBox(height: 32),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: _verifyOtpAndGoToPassword,
              style: ElevatedButton.styleFrom(
                backgroundColor: colorScheme.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
                elevation: 2,
              ),
              child: const Text(
                'Xác nhận OTP',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
              ),
            ),
          ),
          const SizedBox(height: 24),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              TextButton.icon(
                onPressed: () {
                  setState(() {
                    _currentStep = 1;
                    _otpController.clear();
                  });
                },
                icon: const Icon(Icons.edit_outlined, size: 16),
                label: const Text('Thay đổi Email'),
              ),
              _otpCountdown > 0
                  ? Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: Text(
                        'Gửi lại mã sau ${_otpCountdown}s',
                        style: TextStyle(
                          color: Colors.grey.shade500,
                          fontSize: 13,
                        ),
                      ),
                    )
                  : TextButton(
                      onPressed: _isSendingRequest ? null : _sendOtp,
                      child: const Text('Gửi lại mã'),
                    ),
            ],
          ),
        ],
      ),
    );
  }

  // ─── BƯỚC 3: NHẬP MẬT KHẨU MỚI ────────────────────────────
  Widget _buildPasswordStep(ColorScheme colorScheme) {
    return Form(
      key: _passwordFormKey,
      child: Column(
        key: const ValueKey('password_step'),
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(
              color: Colors.green.withOpacity(0.12),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.vpn_key_rounded,
              size: 44,
              color: Colors.green,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Đặt Mật Khẩu Mới',
            style: TextStyle(
              fontSize: 26,
              fontWeight: FontWeight.bold,
              color: colorScheme.primary,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'Nhập mật khẩu mới của bạn bên dưới',
            style: TextStyle(fontSize: 14, color: Colors.grey.shade500),
          ),
          const SizedBox(height: 32),

          TextFormField(
            controller: _passwordController,
            decoration: _inputDeco(
              'Mật khẩu mới',
              Icons.lock_outline,
              suffix: IconButton(
                icon: Icon(
                  _obscurePassword
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined,
                  color: Colors.grey,
                ),
                onPressed: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
              ),
            ),
            obscureText: _obscurePassword,
            validator: _validatePassword,
          ),
          const SizedBox(height: 16),

          TextFormField(
            controller: _confirmPasswordController,
            decoration: _inputDeco(
              'Xác nhận mật khẩu mới',
              Icons.lock_outline,
              suffix: IconButton(
                icon: Icon(
                  _obscureConfirm
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined,
                  color: Colors.grey,
                ),
                onPressed: () =>
                    setState(() => _obscureConfirm = !_obscureConfirm),
              ),
            ),
            obscureText: _obscureConfirm,
            validator: (v) {
              if (v == null || v.isEmpty) return 'Vui lòng xác nhận mật khẩu';
              if (v != _passwordController.text)
                return 'Mật khẩu xác nhận không khớp';
              return null;
            },
          ),
          const SizedBox(height: 32),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: _isSendingRequest ? null : _handleResetPassword,
              style: ElevatedButton.styleFrom(
                backgroundColor: colorScheme.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
                elevation: 2,
              ),
              child: _isSendingRequest
                  ? const SizedBox(
                      height: 22,
                      width: 22,
                      child: CircularProgressIndicator(
                        strokeWidth: 2.5,
                        valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                      ),
                    )
                  : const Text(
                      'Đặt lại mật khẩu',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
            ),
          ),
          const SizedBox(height: 24),

          TextButton.icon(
            onPressed: () {
              setState(() {
                _currentStep = 2;
                _passwordController.clear();
                _confirmPasswordController.clear();
              });
            },
            icon: const Icon(Icons.arrow_back, size: 16),
            label: const Text('Quay lại bước trước'),
          ),
        ],
      ),
    );
  }

  // ─── BƯỚC 4: THÀNH CÔNG ───────────────────────────────────
  Widget _buildSuccessStep(ColorScheme colorScheme) {
    return Column(
      key: const ValueKey('success_step'),
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 90,
          height: 90,
          decoration: const BoxDecoration(
            color: Colors.green,
            shape: BoxShape.circle,
          ),
          child: const Icon(Icons.check_rounded, size: 54, color: Colors.white),
        ),
        const SizedBox(height: 24),
        Text(
          'Thành Công!',
          style: TextStyle(
            fontSize: 28,
            fontWeight: FontWeight.bold,
            color: colorScheme.primary,
          ),
        ),
        const SizedBox(height: 12),
        const Text(
          'Mật khẩu của bạn đã được đặt lại thành công.',
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 16, color: Colors.black87),
        ),
        const SizedBox(height: 4),
        Text(
          'Đang tự động quay lại trang đăng nhập...',
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 14, color: Colors.grey.shade600),
        ),
        const SizedBox(height: 32),
        const SizedBox(
          height: 28,
          width: 28,
          child: CircularProgressIndicator(strokeWidth: 3),
        ),
      ],
    );
  }
}
