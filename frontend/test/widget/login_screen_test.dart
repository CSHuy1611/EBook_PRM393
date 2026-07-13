import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/secure_storage_service.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/login_screen.dart';

class TestAuthProvider extends AuthProvider {
  bool _testLoading = false;

  TestAuthProvider() : super(SecureStorageService());

  @override
  bool get isLoading => _testLoading;

  void setLoading(bool value) {
    _testLoading = value;
    notifyListeners();
  }
}

Widget createLoginScreen({bool loading = false}) {
  final authProvider = TestAuthProvider();
  authProvider.setLoading(loading);

  return MaterialApp(
    home: ChangeNotifierProvider<AuthProvider>.value(
      value: authProvider,
      child: const LoginScreen(),
    ),
  );
}

void main() {
  group('LoginScreen', () {
    testWidgets('renders login screen with all elements', (tester) async {
      await tester.pumpWidget(createLoginScreen());

      expect(find.byType(TextFormField), findsNWidgets(2));
      expect(find.byType(ElevatedButton), findsOneWidget);
      expect(find.text('Đăng nhập'), findsWidgets);
      expect(find.text('Math IBook'), findsOneWidget);
      expect(find.text('Chưa có tài khoản? Đăng ký'), findsOneWidget);
    });

    testWidgets('shows validation error for empty email field', (tester) async {
      await tester.pumpWidget(createLoginScreen());

      await tester.tap(find.byType(ElevatedButton));
      await tester.pumpAndSettle();

      expect(find.text('Vui lòng nhập email'), findsOneWidget);
    });

    testWidgets('shows validation error for empty password field', (tester) async {
      await tester.pumpWidget(createLoginScreen());

      await tester.enterText(find.byType(TextFormField).at(0), 'test@example.com');
      await tester.tap(find.byType(ElevatedButton));
      await tester.pumpAndSettle();

      expect(find.text('Vui lòng nhập mật khẩu'), findsOneWidget);
    });

    testWidgets('shows validation error for invalid email format', (tester) async {
      await tester.pumpWidget(createLoginScreen());

      await tester.enterText(find.byType(TextFormField).at(0), 'notanemail');
      await tester.enterText(find.byType(TextFormField).at(1), 'password123');
      await tester.tap(find.byType(ElevatedButton));
      await tester.pumpAndSettle();

      expect(find.text('Email không hợp lệ'), findsOneWidget);
    });

    testWidgets('does not show validation errors with valid input', (tester) async {
      await tester.pumpWidget(createLoginScreen());

      await tester.enterText(find.byType(TextFormField).at(0), 'test@example.com');
      await tester.enterText(find.byType(TextFormField).at(1), 'password123');
      await tester.tap(find.byType(ElevatedButton));
      await tester.pumpAndSettle();

      expect(find.text('Vui lòng nhập email'), findsNothing);
      expect(find.text('Vui lòng nhập mật khẩu'), findsNothing);
      expect(find.text('Email không hợp lệ'), findsNothing);
    });

    testWidgets('button shows CircularProgressIndicator when loading', (tester) async {
      await tester.pumpWidget(createLoginScreen(loading: true));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Đăng nhập'), findsNothing);
    });

    testWidgets('button is disabled while loading', (tester) async {
      await tester.pumpWidget(createLoginScreen(loading: true));

      final button = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(button.onPressed, isNull);
    });

    testWidgets('register navigation button is present', (tester) async {
      await tester.pumpWidget(createLoginScreen());

      expect(find.byType(TextButton), findsOneWidget);
      expect(find.text('Chưa có tài khoản? Đăng ký'), findsOneWidget);
    });

    testWidgets('email field exists', (tester) async {
      await tester.pumpWidget(createLoginScreen());
      expect(find.byType(TextFormField).at(0), findsOneWidget);
    });
  });
}
