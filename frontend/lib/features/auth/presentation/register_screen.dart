import 'dart:async';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/widgets/math_background.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _otpFormKey = GlobalKey<FormState>();

  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  final _otpController = TextEditingController();

  bool _obscurePassword = true;
  bool _obscureConfirm = true;
  bool _showOtpForm = false;
  int _otpCountdown = 0;
  Timer? _timer;
  bool _isSendingOtp = false;

  @override
  void dispose() {
    _timer?.cancel();
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    _otpController.dispose();
    super.dispose();
  }

  void _startCountdown() {
    setState(() { _otpCountdown = 60; });
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_otpCountdown == 0) { timer.cancel(); } else { setState(() { _otpCountdown--; }); }
    });
  }

  String? _validateEmail(String? value) {
    if (value == null || value.isEmpty) return 'Vui lòng nhập email';
    if (!RegExp(r'^[^@]+@[^@]+\.[^@]+$').hasMatch(value)) return 'Email không hợp lệ';
    return null;
  }

  String? _validatePassword(String? value) {
    if (value == null || value.isEmpty) return 'Vui lòng nhập mật khẩu';
    if (value.length < 6) return 'Mật khẩu tối thiểu 6 ký tự';
    if (!value.contains(RegExp(r'[A-Z]'))) return 'Cần ít nhất 1 chữ hoa';
    if (!value.contains(RegExp(r'[0-9]'))) return 'Cần ít nhất 1 chữ số';
    return null;
  }

  Future<void> _sendOtpAndGoToVerification() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _isSendingOtp = true; });
    try {
      await context.read<AuthProvider>().requestOtp(_emailController.text.trim());
      _startCountdown();
      setState(() { _showOtpForm = true; _isSendingOtp = false; });
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mã OTP đã được gửi đến Email của bạn.'), backgroundColor: Colors.green),
      );
    } catch (e) {
      setState(() { _isSendingOtp = false; });
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()), backgroundColor: Colors.red),
      );
    }
  }

  Future<void> _resendOtp() async {
    try {
      await context.read<AuthProvider>().requestOtp(_emailController.text.trim());
      _startCountdown();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đã gửi lại mã OTP mới.'), backgroundColor: Colors.blue),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()), backgroundColor: Colors.red),
      );
    }
  }

  Future<void> _handleRegister() async {
    if (!_otpFormKey.currentState!.validate()) return;
    try {
      await context.read<AuthProvider>().register(
        _nameController.text.trim(), _emailController.text.trim(),
        _passwordController.text, _confirmPasswordController.text, _otpController.text.trim(),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString()), backgroundColor: Colors.red),
      );
    }
  }

  InputDecoration _inputDeco(String label, IconData icon, {Widget? suffix, String? hint}) {
    final colorScheme = Theme.of(context).colorScheme;
    return InputDecoration(
      labelText: label,
      hintText: hint,
      prefixIcon: Icon(icon),
      suffixIcon: suffix,
      filled: true,
      fillColor: Colors.white.withOpacity(0.9),
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: Colors.grey.shade300)),
      enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: Colors.grey.shade300)),
      focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.primary, width: 2)),
    );
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: MathBackground(
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
              child: AnimatedSwitcher(
                duration: const Duration(milliseconds: 250),
                child: _showOtpForm
                    ? _buildOtpForm(colorScheme)
                    : _buildRegisterForm(colorScheme),
              ),
            ),
          ),
        ),
      ),
    );
  }

  // ─── BƯỚC 1: FORM ĐĂNG KÝ ─────────────────────────────
  Widget _buildRegisterForm(ColorScheme colorScheme) {
    final authProvider = context.watch<AuthProvider>();
    return Form(
      key: _formKey,
      child: Column(
        key: const ValueKey('register'),
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 80, height: 80,
            decoration: BoxDecoration(color: colorScheme.primary.withOpacity(0.12), shape: BoxShape.circle),
            child: Icon(Icons.menu_book_rounded, size: 44, color: colorScheme.primary),
          ),
          const SizedBox(height: 16),
          Text('Math IBook', style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: colorScheme.primary)),
          const SizedBox(height: 4),
          Text('Tạo tài khoản học tập mới', style: TextStyle(fontSize: 14, color: Colors.grey.shade500)),
          const SizedBox(height: 40),

          TextFormField(
            controller: _nameController,
            decoration: _inputDeco('Họ tên học sinh', Icons.person_outline),
            validator: (v) {
              if (v == null || v.trim().isEmpty) return 'Vui lòng nhập họ tên';
              if (v.trim().length < 2) return 'Họ tên tối thiểu 2 ký tự';
              return null;
            },
          ),
          const SizedBox(height: 16),

          TextFormField(
            controller: _emailController,
            decoration: _inputDeco('Email', Icons.email_outlined, hint: 'example@gmail.com'),
            keyboardType: TextInputType.emailAddress,
            validator: _validateEmail,
          ),
          const SizedBox(height: 16),

          TextFormField(
            controller: _passwordController,
            decoration: _inputDeco('Mật khẩu', Icons.lock_outline,
              suffix: IconButton(
                icon: Icon(_obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined, color: Colors.grey),
                onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
              ),
            ),
            obscureText: _obscurePassword,
            validator: _validatePassword,
          ),
          const SizedBox(height: 16),

          TextFormField(
            controller: _confirmPasswordController,
            decoration: _inputDeco('Xác nhận mật khẩu', Icons.lock_outline,
              suffix: IconButton(
                icon: Icon(_obscureConfirm ? Icons.visibility_outlined : Icons.visibility_off_outlined, color: Colors.grey),
                onPressed: () => setState(() => _obscureConfirm = !_obscureConfirm),
              ),
            ),
            obscureText: _obscureConfirm,
            validator: (v) {
              if (v == null || v.isEmpty) return 'Vui lòng xác nhận mật khẩu';
              if (v != _passwordController.text) return 'Mật khẩu không khớp';
              return null;
            },
          ),
          const SizedBox(height: 32),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: _isSendingOtp ? null : _sendOtpAndGoToVerification,
              style: ElevatedButton.styleFrom(
                backgroundColor: colorScheme.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                elevation: 2,
              ),
              child: _isSendingOtp
                  ? const SizedBox(height: 22, width: 22, child: CircularProgressIndicator(strokeWidth: 2.5, valueColor: AlwaysStoppedAnimation<Color>(Colors.white)))
                  : const Text('Đăng ký', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            ),
          ),
          const SizedBox(height: 24),

          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text('Đã có tài khoản? ', style: TextStyle(color: Colors.grey.shade600, fontSize: 14)),
              GestureDetector(
                onTap: () => context.go('/login'),
                child: MouseRegion(
                  cursor: SystemMouseCursors.click,
                  child: Text('Đăng nhập ngay', style: TextStyle(color: colorScheme.primary, fontWeight: FontWeight.bold, fontSize: 14)),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // ─── BƯỚC 2: FORM NHẬP OTP ────────────────────────────
  Widget _buildOtpForm(ColorScheme colorScheme) {
    final authProvider = context.watch<AuthProvider>();
    return Form(
      key: _otpFormKey,
      child: Column(
        key: const ValueKey('otp'),
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 80, height: 80,
            decoration: BoxDecoration(color: Colors.orange.withOpacity(0.12), shape: BoxShape.circle),
            child: const Icon(Icons.mark_email_unread_outlined, size: 44, color: Colors.orange),
          ),
          const SizedBox(height: 16),
          Text('Xác thực Email', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: colorScheme.primary)),
          const SizedBox(height: 8),
          RichText(
            textAlign: TextAlign.center,
            text: TextSpan(
              style: TextStyle(fontSize: 14, color: Colors.grey.shade600, height: 1.5),
              children: [
                const TextSpan(text: 'Mã OTP 6 chữ số đã gửi đến\n'),
                TextSpan(text: _emailController.text.trim(), style: TextStyle(fontWeight: FontWeight.bold, color: colorScheme.primary)),
              ],
            ),
          ),
          const SizedBox(height: 32),

          TextFormField(
            controller: _otpController,
            decoration: _inputDeco('Mã OTP', Icons.security),
            keyboardType: TextInputType.number,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 28, fontWeight: FontWeight.bold, letterSpacing: 10),
            validator: (v) {
              if (v == null || v.isEmpty) return 'Vui lòng nhập mã OTP';
              if (v.length != 6) return 'Mã OTP phải có đúng 6 chữ số';
              return null;
            },
          ),
          const SizedBox(height: 16),

          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                _otpCountdown > 0 ? 'Gửi lại mã sau ${_otpCountdown}s' : 'Không nhận được mã? ',
                style: TextStyle(color: Colors.grey.shade500, fontSize: 13),
              ),
              if (_otpCountdown == 0)
                GestureDetector(
                  onTap: _resendOtp,
                  child: MouseRegion(
                    cursor: SystemMouseCursors.click,
                    child: Text('Gửi lại', style: TextStyle(color: colorScheme.primary, fontWeight: FontWeight.bold, fontSize: 13)),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 32),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: ElevatedButton(
              onPressed: authProvider.isLoading ? null : _handleRegister,
              style: ElevatedButton.styleFrom(
                backgroundColor: colorScheme.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                elevation: 2,
              ),
              child: authProvider.isLoading
                  ? const SizedBox(height: 22, width: 22, child: CircularProgressIndicator(strokeWidth: 2.5, valueColor: AlwaysStoppedAnimation<Color>(Colors.white)))
                  : const Text('Xác nhận đăng ký', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            ),
          ),
          const SizedBox(height: 16),

          TextButton.icon(
            onPressed: () { setState(() { _showOtpForm = false; _otpController.clear(); }); },
            icon: const Icon(Icons.arrow_back, size: 16),
            label: const Text('Quay lại sửa thông tin'),
          ),
        ],
      ),
    );
  }
}
