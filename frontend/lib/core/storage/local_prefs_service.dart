import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

class LocalPrefsService extends ChangeNotifier {
  static final LocalPrefsService _instance = LocalPrefsService._internal();
  factory LocalPrefsService() => _instance;
  LocalPrefsService._internal();

  SharedPreferences? _prefs;

  Future<void> init() async {
    _prefs = await SharedPreferences.getInstance();
  }

  Future<void> setFontScale(double scale) async {
    await _prefs?.setDouble('font_scale', scale);
    notifyListeners();
  }

  double getFontScale() {
    return _prefs?.getDouble('font_scale') ?? 1.0;
  }

  Future<void> setThemeMode(String mode) async {
    await _prefs?.setString('theme_mode', mode);
    notifyListeners();
  }

  String getThemeMode() {
    return _prefs?.getString('theme_mode') ?? 'light';
  }

  Future<void> setString(String key, String value) async {
    await _prefs?.setString(key, value);
    notifyListeners();
  }

  String? getString(String key) {
    return _prefs?.getString(key);
  }
}
