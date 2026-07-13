import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/storage/secure_storage_service.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/main.dart' as app;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  setUp(() async {
    final storage = SecureStorageService();
    await storage.clearAll();
  });

  group('App Integration Tests', () {
    testWidgets('Test A: Login as Student and navigate', (tester) async {
      await tester.pumpWidget(const app.MathIBookApp());
      await tester.pumpAndSettle();

      // Navigate to login
      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle();

      // Fill in student credentials
      await tester.enterText(find.byType(TextFormField).at(0), 'student@mathibook.vn');
      await tester.enterText(find.byType(TextFormField).at(1), 'Student@123');

      // Tap login button
      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle(const Duration(seconds: 5));

      // Verify we're on student home
      expect(find.text('Trang chủ'), findsWidgets);
    });

    testWidgets('Test B: Login as Admin and verify AdminShell', (tester) async {
      await tester.pumpWidget(const app.MathIBookApp());
      await tester.pumpAndSettle();

      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextFormField).at(0), 'admin@mathibook.vn');
      await tester.enterText(find.byType(TextFormField).at(1), 'Admin@123');

      await tester.tap(find.text('Đăng nhập'));
      await tester.pumpAndSettle(const Duration(seconds: 5));

      // Verify admin dashboard is shown
      expect(find.text('Tổng quan'), findsWidgets);
    });
  });
}
