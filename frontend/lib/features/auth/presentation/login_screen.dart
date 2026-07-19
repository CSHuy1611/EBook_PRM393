import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/widgets/math_background.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  String? _validateEmail(String? value) {
    if (value == null || value.isEmpty) return 'Vui lòng nhập email';
    final emailRegex = RegExp(r'^[^@]+@[^@]+\.[^@]+$');
    if (!emailRegex.hasMatch(value)) return 'Email không hợp lệ';
    return null;
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) return;
    final authProvider = context.read<AuthProvider>();
    try {
      await authProvider.login(_emailController.text.trim(), _passwordController.text);
    } catch (e) {
      if (!mounted) return;
      final message = e is DioException ? ApiClient.mapDioErrorToMessage(e) : e.toString();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(message), backgroundColor: Colors.red),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: MathBackground(
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
              child: Form(
                key: _formKey,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // Logo
                    Container(
                      width: 80, height: 80,
                      decoration: BoxDecoration(
                        color: colorScheme.primary.withOpacity(0.12),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.menu_book_rounded, size: 44, color: colorScheme.primary),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'Math IBook',
                      style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: colorScheme.primary),
                    ),
                    const SizedBox(height: 4),
                    Text('Sách Toán 8 Tương Tác', style: TextStyle(fontSize: 14, color: Colors.grey.shade500)),
                    const SizedBox(height: 40),

                    // Email - FULL WIDTH
                    TextFormField(
                      controller: _emailController,
                      style: TextStyle(color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        labelText: 'Email',
                        labelStyle: TextStyle(color: colorScheme.onSurface.withOpacity(0.7)),
                        hintText: 'example@gmail.com',
                        prefixIcon: Icon(Icons.email_outlined, color: colorScheme.primary),
                        filled: true,
                        fillColor: colorScheme.surfaceContainerHighest.withOpacity(0.85),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.outline.withOpacity(0.5))),
                        enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.outline.withOpacity(0.5))),
                        focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.primary, width: 2)),
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
                        labelStyle: TextStyle(color: colorScheme.onSurface.withOpacity(0.7)),
                        prefixIcon: Icon(Icons.lock_outline, color: colorScheme.primary),
                        filled: true,
                        fillColor: colorScheme.surfaceContainerHighest.withOpacity(0.85),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.outline.withOpacity(0.5))),
                        enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.outline.withOpacity(0.5))),
                        focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide(color: colorScheme.primary, width: 2)),
                        suffixIcon: IconButton(
                          icon: Icon(_obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined, color: colorScheme.onSurface.withOpacity(0.6)),
                          onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                        ),
                      ),
                      obscureText: _obscurePassword,
                      validator: (value) {
                        if (value == null || value.isEmpty) return 'Vui lòng nhập mật khẩu';
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
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                          elevation: 2,
                        ),
                        child: authProvider.isLoading
                            ? const SizedBox(height: 22, width: 22, child: CircularProgressIndicator(strokeWidth: 2.5, valueColor: AlwaysStoppedAnimation<Color>(Colors.white)))
                            : const Text('Đăng nhập', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Register link
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text('Chưa có tài khoản? ', style: TextStyle(color: Colors.grey.shade600, fontSize: 14)),
                        GestureDetector(
                          onTap: () => context.go('/register'),
                          child: MouseRegion(
                            cursor: SystemMouseCursors.click,
                            child: Text('Đăng ký ngay', style: TextStyle(color: colorScheme.primary, fontWeight: FontWeight.bold, fontSize: 14)),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
