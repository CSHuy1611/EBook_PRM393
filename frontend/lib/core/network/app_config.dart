import 'package:flutter/foundation.dart' show kIsWeb;
import 'dart:io' show Platform;

class AppConfig {
  static String get baseUrl {
    const override = String.fromEnvironment('API_BASE_URL');
    if (override.isNotEmpty) return override;
    if (kIsWeb) return 'http://localhost:5000/api';
    if (Platform.isAndroid) return 'http://10.0.2.2:5000/api';
    if (Platform.isWindows) return 'http://localhost:5000/api';
    if (Platform.isIOS) return 'http://localhost:5000/api';
    return 'http://localhost:5000/api';
  }

  static String get rootUrl => baseUrl.replaceAll(RegExp(r'/api$'), '');
}
