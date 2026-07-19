import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/models/student_feature_models.dart';
import 'package:math_ibook/core/network/student_feature_api.dart';
import 'package:math_ibook/core/storage/local_prefs_service.dart';
import 'package:math_ibook/core/widgets/error_widget.dart';
import 'package:math_ibook/core/widgets/loading_widget.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:dio/dio.dart';
import 'package:math_ibook/core/network/api_client.dart';
import 'package:math_ibook/core/network/app_config.dart';
import 'package:image_picker/image_picker.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});
  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  // _profile chỉ có giá trị sau khi GET /profile/me thành công.
  StudentProfileModel? _profile;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    // Dùng chung cho lần mở đầu, retry và pull-to-refresh.
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      // API xác định hồ sơ qua JWT, màn hình không truyền userId.
      final profile = await StudentFeatureApi.instance.getProfile();
      if (mounted)
        setState(() {
          _profile = profile;
          _loading = false;
        });
    } catch (error) {
      if (mounted)
        setState(() {
          _error = error is DioException
              ? ApiClient.mapDioErrorToMessage(error)
              : error.toString();
          _loading = false;
        });
    }
  }

  String _getFullAvatarUrl(String url) {
    if (url.startsWith('http')) return url;
    return '${AppConfig.rootUrl}$url';
  }

  Future<void> _pickAndUploadAvatar() async {
    final picker = ImagePicker();
    final pickedFile = await picker.pickImage(source: ImageSource.gallery);
    if (pickedFile == null) return;
    
    setState(() => _loading = true);
    try {
      final bytes = await pickedFile.readAsBytes();
      final updated = await StudentFeatureApi.instance.uploadAvatar(bytes, pickedFile.name);
      if (mounted) {
        setState(() {
          _profile = updated;
          _loading = false;
        });
        
        // Cập nhật AuthProvider để hiển thị avatar toàn hệ thống (Home, Leaderboard)
        final authProvider = context.read<AuthProvider>();
        if (authProvider.currentUser != null) {
          authProvider.updateUser(authProvider.currentUser!.copyWith(avatarUrl: updated.avatarUrl));
        }
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã cập nhật ảnh đại diện')),
        );
      }
    } catch (error) {
      if (mounted) {
        setState(() => _loading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể tải ảnh: $error')),
        );
      }
    }
  }

  Future<void> _editProfile() async {
    // Dữ liệu hiện tại được đưa vào controller để dialog có giá trị ban đầu.
    final profile = _profile!;
    final name = TextEditingController(text: profile.name);
    final saved = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cập nhật hồ sơ'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: name,
                decoration: const InputDecoration(labelText: 'Họ và tên'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Lưu'),
          ),
        ],
      ),
    );
    // Chỉ gọi API nếu người dùng bấm Lưu; đóng/Hủy trả null hoặc false.
    if (saved != true) return;
    try {
      // Trim trước khi gửi; server tiếp tục validate để bảo đảm an toàn.
      final updated = await StudentFeatureApi.instance.updateProfile(
        name: name.text.trim(),
        avatarUrl: profile.avatarUrl,
      );
      // Response PUT là profile đầy đủ nên thay state trực tiếp, không cần GET lần hai.
      if (mounted) {
        setState(() => _profile = updated);
        
        final authProvider = context.read<AuthProvider>();
        if (authProvider.currentUser != null) {
          authProvider.updateUser(authProvider.currentUser!.copyWith(name: updated.name));
        }
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Đã cập nhật hồ sơ')));
      }
    } catch (error) {
      if (mounted)
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('Không thể cập nhật: $error')));
    }
  }

  Future<void> _changePassword() async {
    // Ba controller tách biệt để gửi đúng current/new/confirm cho server.
    final current = TextEditingController();
    final next = TextEditingController();
    final confirm = TextEditingController();
    final submitted = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Đổi mật khẩu'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: current,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Mật khẩu hiện tại',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: next,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Mật khẩu mới'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: confirm,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Xác nhận mật khẩu mới',
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Đổi mật khẩu'),
          ),
        ],
      ),
    );
    // Không gửi request nếu người dùng hủy dialog.
    if (submitted != true) return;
    try {
      await StudentFeatureApi.instance.changePassword(
        currentPassword: current.text,
        newPassword: next.text,
        confirmNewPassword: confirm.text,
      );
      if (!mounted) return;
      // Backend đã đổi password và revoke refresh token, frontend logout phiên hiện tại.
      await context.read<AuthProvider>().logout();
      if (mounted)
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.'),
          ),
        );
    } catch (error) {
      if (mounted)
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể đổi mật khẩu: $error')),
        );
    }
  }

  Future<void> _confirmLogout() async {
    // Dialog xác nhận ngăn thao tác chạm nhầm làm mất phiên đăng nhập.
    final approved = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc muốn đăng xuất không?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
    if (approved == true && mounted)
      await context.read<AuthProvider>().logout();
  }

  @override
  Widget build(BuildContext context) {
    // Loading/error được xử lý trước khi dùng toán tử ! với _profile.
    if (_loading) return const AppLoadingWidget(message: 'Đang tải hồ sơ...');
    if (_error != null) return AppErrorWidget(message: _error!, onRetry: _load);
    final p = _profile!;
    // watch giúp theme/font scale thay đổi và rebuild ngay trên màn hình.
    final prefs = context.watch<LocalPrefsService>();
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Row(
            children: [
              // Không có avatar URL thì dùng chữ cái đầu tên làm fallback.
              Stack(
                children: [
                  CircleAvatar(
                    radius: 38,
                    backgroundImage: p.avatarUrl?.isNotEmpty == true
                        ? NetworkImage(_getFullAvatarUrl(p.avatarUrl!))
                        : null,
                    child: p.avatarUrl?.isNotEmpty == true
                        ? null
                        : Text(
                            p.name.isEmpty ? '?' : p.name[0].toUpperCase(),
                            style: const TextStyle(fontSize: 28),
                          ),
                  ),
                  Positioned(
                    bottom: 0,
                    right: 0,
                    child: InkWell(
                      onTap: _pickAndUploadAvatar,
                      child: Container(
                        padding: const EdgeInsets.all(4),
                        decoration: BoxDecoration(
                          color: Theme.of(context).colorScheme.primary,
                          shape: BoxShape.circle,
                        ),
                        child: Icon(
                          Icons.camera_alt,
                          size: 16,
                          color: Theme.of(context).colorScheme.onPrimary,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      p.name,
                      style: Theme.of(context).textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.bold),
                    ),
                    Text(p.email),
                  ],
                ),
              ),
              IconButton(
                onPressed: _editProfile,
                icon: const Icon(Icons.edit),
                tooltip: 'Chỉnh sửa hồ sơ',
              ),
            ],
          ),
          const SizedBox(height: 24),
          _Section(
            title: 'Thành tích',
            child: Wrap(
              spacing: 10,
              runSpacing: 10,
              children: [
                // Các metric vừa hiển thị thống kê vừa là shortcut đến feature tương ứng.
                _Metric(
                  label: 'Xu',
                  value: '${p.coins}',
                  icon: Icons.monetization_on,
                  onTap: () => context.push('/student/coins'),
                ),
                _Metric(
                  label: 'Huy hiệu',
                  value: '${p.badgeCount}',
                  icon: Icons.workspace_premium,
                  onTap: () => context.push('/student/badges'),
                ),
                _Metric(
                  label: 'Thứ hạng',
                  value: p.rank == null ? '--' : '#${p.rank}',
                  icon: Icons.leaderboard,
                  onTap: () => context.go('/student/leaderboard'),
                ),
                _Metric(
                  label: 'Bài / Chương',
                  value: '${p.completedLessons}/${p.completedChapters}',
                  icon: Icons.menu_book,
                ),
                _Metric(
                  label: 'Điểm TB',
                  value: p.averageScore.toStringAsFixed(1),
                  icon: Icons.analytics,
                ),
                _Metric(
                  label: 'Điểm tốt nhất',
                  value: p.bestScore.toStringAsFixed(1),
                  icon: Icons.star,
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          _Section(
            title: 'Tài khoản',
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.lock_outline),
                  title: const Text('Đổi mật khẩu'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: _changePassword,
                ),
                ListTile(
                  leading: const Icon(Icons.sync),
                  title: const Text('Ngoại tuyến và đồng bộ'),
                  subtitle: const Text('Xem dữ liệu đang chờ đồng bộ'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push('/student/offline-sync'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          _Section(
            title: 'Cài đặt hiển thị',
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.text_fields),
                  title: const Text('Cỡ chữ'),
                  subtitle: Slider(
                    value: prefs.getFontScale(),
                    min: .8,
                    max: 1.5,
                    divisions: 7,
                    label: '${(prefs.getFontScale() * 100).round()}%',
                    onChanged: prefs.setFontScale,
                  ),
                ),
                SwitchListTile(
                  secondary: const Icon(Icons.dark_mode),
                  title: const Text('Chế độ tối'),
                  value: prefs.getThemeMode() == 'dark',
                  onChanged: (value) =>
                      prefs.setThemeMode(value ? 'dark' : 'light'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          OutlinedButton.icon(
            onPressed: _confirmLogout,
            icon: const Icon(Icons.logout, color: Colors.red),
            label: const Text('Đăng xuất', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
  }
}

class _Section extends StatelessWidget {
  final String title;
  final Widget child;

  const _Section({required this.title, required this.child});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            child,
          ],
        ),
      ),
    );
  }
}

class _Metric extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final VoidCallback? onTap;

  const _Metric({
    required this.label,
    required this.value,
    required this.icon,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 150,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(10),
        child: Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surfaceContainerHighest,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Row(
            children: [
              Icon(icon, size: 21),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      value,
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    Text(
                      label,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
