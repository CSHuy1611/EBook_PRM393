import 'package:flutter/foundation.dart';

class ProgressNotifier extends ChangeNotifier {
  int _version = 0;
  int get version => _version;

  void notifyProgressChanged() {
    _version++;
    notifyListeners();
  }
}
