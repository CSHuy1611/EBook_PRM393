import 'package:flutter/foundation.dart';

class AppConfig {
  static String get baseUrl {
    const override = String.fromEnvironment('API_BASE_URL');
    if (override.isNotEmpty) return override;
    if (kIsWeb) return 'http://127.0.0.1:5000/api';
    if (defaultTargetPlatform == TargetPlatform.android) return 'http://10.0.2.2:5000/api';
    if (defaultTargetPlatform == TargetPlatform.windows) return 'http://127.0.0.1:5000/api';
    if (defaultTargetPlatform == TargetPlatform.iOS) return 'http://127.0.0.1:5000/api';
    return 'http://127.0.0.1:5000/api';
  }

  static String get rootUrl => baseUrl.replaceAll(RegExp(r'/api$'), '');
}
