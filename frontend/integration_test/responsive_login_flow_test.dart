import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:math_ibook/core/storage/secure_storage_service.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/login_screen.dart';
import 'package:provider/provider.dart';

class _IntegrationAuthProvider extends AuthProvider {
  _IntegrationAuthProvider() : super(SecureStorageService());
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('login screen remains usable after device rotation', (
    tester,
  ) async {
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final authProvider = _IntegrationAuthProvider();
    addTearDown(authProvider.dispose);

    await tester.binding.setSurfaceSize(const Size(320, 568));
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: authProvider,
        child: const MaterialApp(home: LoginScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(find.byType(ElevatedButton), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.binding.setSurfaceSize(const Size(568, 320));
    await tester.pumpAndSettle();

    final loginButton = tester.getSize(find.byType(ElevatedButton));
    expect(loginButton.width, lessThanOrEqualTo(460));
    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(tester.takeException(), isNull);
  });
}
