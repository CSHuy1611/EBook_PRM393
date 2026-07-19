import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:dio/dio.dart';

class StudentFeatureApi {
  // Lớp adapter gom endpoint của Student để Screen không phụ thuộc trực tiếp Dio.
  StudentFeatureApi._();
  static final instance = StudentFeatureApi._();

  Future<CoinHistoryModel> getCoins({int page = 1, int pageSize = 20}) async {
    // Backend phân trang lịch sử xu; pageSize mặc định 20 để UI tải theo cuộn.
    final response = await ApiClient.instance.get<Map<String, dynamic>>(
      '/coins',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    // Chuyển JSON camelCase từ C# DTO thành model Dart có kiểu rõ ràng.
    return CoinHistoryModel.fromJson(response.data ?? const {});
  }

  Future<BadgeCollectionModel> getBadges() async {
    // Lấy toàn bộ badge cùng trạng thái Earned/InProgress/Locked.
    final response = await ApiClient.instance.get<Map<String, dynamic>>(
      '/badges',
    );
    return BadgeCollectionModel.fromJson(response.data ?? const {});
  }

  Future<void> reconcileBadges() async {
    // Yêu cầu server xét lại điều kiện và trao các badge còn thiếu trước khi hiển thị.
    await ApiClient.instance.post('/badges/reconcile');
  }

  Future<LeaderboardModel> getLeaderboard() async {
    // Response gồm Top 100, Student hiện tại và thời điểm cập nhật.
    final response = await ApiClient.instance.get<Map<String, dynamic>>(
      '/leaderboard',
    );
    return LeaderboardModel.fromJson(response.data ?? const {});
  }

  Future<StudentProfileModel> getProfile() async {
    // /profile/me xác định Student bằng userId trong JWT, không nhận userId từ UI.
    final response = await ApiClient.instance.get<Map<String, dynamic>>(
      '/profile/me',
    );
    return StudentProfileModel.fromJson(response.data ?? const {});
  }

  Future<StudentProfileModel> updateProfile({
    required String name,
    String? avatarUrl,
  }) async {
    // Chỉ gửi các trường Student được phép sửa; email/coins/role không nằm trong body.
    final response = await ApiClient.instance.put<Map<String, dynamic>>(
      '/profile/me',
      data: {'name': name, 'avatarUrl': avatarUrl},
    );
    return StudentProfileModel.fromJson(response.data ?? const {});
  }

  Future<StudentProfileModel> uploadAvatar(List<int> bytes, String filename) async {
    final formData = FormData.fromMap({
      'file': MultipartFile.fromBytes(bytes, filename: filename),
    });
    final response = await ApiClient.instance.post<Map<String, dynamic>>(
      '/profile/avatar',
      data: formData,
    );
    return StudentProfileModel.fromJson(response.data ?? const {});
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
    required String confirmNewPassword,
  }) async {
    // Server tự xác minh mật khẩu cũ, độ mạnh và thu hồi refresh token sau khi đổi.
    await ApiClient.instance.post(
      '/profile/change-password',
      data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'confirmNewPassword': confirmNewPassword,
      },
    );
  }
}
