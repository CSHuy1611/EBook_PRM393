class UserModel {
  final String id;
  final String name;
  final String email;
  final String role;
  final int coins;
  final String? avatarUrl;

  UserModel({
    required this.id,
    required this.name,
    required this.email,
    required this.role,
    this.coins = 0,
    this.avatarUrl,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) => UserModel(
        id: json['id'] ?? '',
        name: json['name'] ?? '',
        email: json['email'] ?? '',
        role: json['role'] ?? 'Student',
        coins: json['coins'] ?? 0,
        avatarUrl: json['avatarUrl'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'email': email,
        'role': role,
        'coins': coins,
        'avatarUrl': avatarUrl,
      };

  UserModel copyWith({
    String? id,
    String? name,
    String? email,
    String? role,
    int? coins,
    String? avatarUrl,
  }) => UserModel(
        id: id ?? this.id,
        name: name ?? this.name,
        email: email ?? this.email,
        role: role ?? this.role,
        coins: coins ?? this.coins,
        avatarUrl: avatarUrl ?? this.avatarUrl,
      );
}
