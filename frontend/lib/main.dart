import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'core/network/api_client.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/storage/local_prefs_service.dart';
import 'core/storage/local_db_service.dart';
import 'core/theme/app_theme.dart';
import 'core/router/app_router.dart';
import 'core/progress/progress_notifier.dart';
import 'features/auth/domain/auth_provider.dart';

late final GoRouter appRouter;

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  FlutterError.onError = (details) {
    debugPrint('FlutterError: ${details.exception}');
    FlutterError.presentError(details);
  };
  ErrorWidget.builder = (details) {
    debugPrint('ErrorWidget: ${details.exception}');
    return Material(child: Center(child: Text('${details.exception}')));
  };

  final storage = SecureStorageService();
  final prefs = LocalPrefsService();
  final db = LocalDbService();
  try {
    await db.init();
    await prefs.init();
  } catch (e, stack) {
    debugPrint('Init error: $e');
    FlutterError.reportError(FlutterErrorDetails(exception: e, stack: stack));
  }

  ApiClient.init(storage);

  final authProvider = AuthProvider(storage);

  appRouter = createAppRouter(authProvider);

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: authProvider),
        ChangeNotifierProvider.value(value: prefs),
        Provider.value(value: db),
        ChangeNotifierProvider(create: (_) => ProgressNotifier()),
      ],
      child: const MathIBookApp(),
    ),
  );
}

class MathIBookApp extends StatelessWidget {
  const MathIBookApp({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<LocalPrefsService>(
      builder: (context, prefs, _) {
        return MaterialApp.router(
          title: 'Math IBook',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: prefs.getThemeMode() == 'dark' ? ThemeMode.dark : ThemeMode.light,
          routerConfig: appRouter,
        );
      },
    );
  }
}
