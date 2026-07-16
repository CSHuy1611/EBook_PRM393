import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/api_client.dart';

class StudentFeatureApi {
  StudentFeatureApi._();
  static final instance = StudentFeatureApi._();

  Future<CoinHistoryModel> getCoins({int page = 1, int pageSize = 20}) async {
    final response = await ApiClient.instance.get<Map<String, dynamic>>(
      '/coins',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return CoinHistoryModel.fromJson(response.data ?? const {});
  }

  Future<BadgeCollectionModel> getBadges() async {
    final response = await ApiClient.instance.get<Map<String, dynamic>>('/badges');
    return BadgeCollectionModel.fromJson(response.data ?? const {});
  }

  Future<void> reconcileBadges() async {
    await ApiClient.instance.post('/badges/reconcile');
  }

  Future<LeaderboardModel> getLeaderboard() async {
    final response = await ApiClient.instance.get<Map<String, dynamic>>('/leaderboard');
    return LeaderboardModel.fromJson(response.data ?? const {});
  }

  Future<StudentProfileModel> getProfile() async {
    final response = await ApiClient.instance.get<Map<String, dynamic>>('/profile/me');
    return StudentProfileModel.fromJson(response.data ?? const {});
  }

  Future<StudentProfileModel> updateProfile({
    required String name,
    String? avatarUrl,
  }) async {
    final response = await ApiClient.instance.put<Map<String, dynamic>>(
      '/profile/me',
      data: {'name': name, 'avatarUrl': avatarUrl},
    );
    return StudentProfileModel.fromJson(response.data ?? const {});
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
    required String confirmNewPassword,
  }) async {
    await ApiClient.instance.post('/profile/change-password', data: {
      'currentPassword': currentPassword,
      'newPassword': newPassword,
      'confirmNewPassword': confirmNewPassword,
    });
  }
}
