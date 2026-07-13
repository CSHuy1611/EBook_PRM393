class UserModel {
  final String id;
  final String name;
  final String email;
  final String role;
  final int coins;

  UserModel({
    required this.id,
    required this.name,
    required this.email,
    required this.role,
    this.coins = 0,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) => UserModel(
        id: json['id'] ?? '',
        name: json['name'] ?? '',
        email: json['email'] ?? '',
        role: json['role'] ?? 'Student',
        coins: json['coins'] ?? 0,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'email': email,
        'role': role,
        'coins': coins,
      };

  UserModel copyWith({int? coins}) => UserModel(
        id: id,
        name: name,
        email: email,
        role: role,
        coins: coins ?? this.coins,
      );
}
