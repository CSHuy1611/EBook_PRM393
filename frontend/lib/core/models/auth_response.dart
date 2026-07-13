import 'user_model.dart';

class AuthResponse {
  final String token;
  final String refreshToken;
  final UserModel user;

  AuthResponse({
    required this.token,
    required this.refreshToken,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
        token: json['accessToken'] ?? json['token'] ?? '',
        refreshToken: json['refreshToken'] ?? '',
        user: UserModel.fromJson(json['user'] ?? {}),
      );

  Map<String, dynamic> toJson() => {
        'accessToken': token,
        'refreshToken': refreshToken,
        'user': user.toJson(),
      };
}
