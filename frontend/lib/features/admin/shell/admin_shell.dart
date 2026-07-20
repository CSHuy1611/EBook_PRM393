import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/core/layout/responsive_layout.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';

class AdminShell extends StatelessWidget {
  final StatefulNavigationShell navigationShell;

  const AdminShell({super.key, required this.navigationShell});

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().currentUser;

    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth > 900) {
          return _buildNavigationRail(context, user, constraints.maxWidth);
        }
        return _buildDrawer(context, user);
      },
    );
  }

  void _logout(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc muốn đăng xuất không?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () {
              Navigator.pop(ctx);
              context.read<AuthProvider>().logout();
            },
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
  }

  Widget _buildNavigationRail(
    BuildContext context,
    dynamic user,
    double maxWidth,
  ) {
    final bool isExtended = maxWidth >= 1100;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Math IBook - Quản trị'),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: Center(
              child: Text(
                user?.name ?? 'Admin',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Đăng xuất',
            onPressed: () => _logout(context),
          ),
        ],
      ),
      body: Row(
        children: [
          NavigationRail(
            extended: isExtended,
            minExtendedWidth: 200,
            selectedIndex: navigationShell.currentIndex,
            onDestinationSelected: (index) {
              navigationShell.goBranch(index, initialLocation: true);
            },
            labelType: isExtended
                ? NavigationRailLabelType.none
                : NavigationRailLabelType.all,
            destinations: const [
              NavigationRailDestination(
                icon: Icon(Icons.dashboard),
                label: Text(
                  'Bảng điều khiển',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.book),
                label: Text(
                  'Chương học',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.menu_book),
                label: Text(
                  'Bài học',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.help),
                label: Text(
                  'Câu hỏi',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.quiz),
                label: Text(
                  'Bài Quiz',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.emoji_events),
                label: Text(
                  'Huy hiệu',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.people),
                label: Text(
                  'Học sinh',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.policy),
                label: Text(
                  'CS. Thưởng',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.notifications),
                label: Text(
                  'Thông báo',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              NavigationRailDestination(
                icon: Icon(Icons.settings),
                label: Text(
                  'Cài đặt',
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const VerticalDivider(width: 1),
          Expanded(
            child: ResponsiveContent(maxWidth: 1440, child: navigationShell),
          ),
        ],
      ),
    );
  }

  Widget _buildDrawer(BuildContext context, dynamic user) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Math IBook - Quản trị'),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: Center(
              child: Text(
                user?.name ?? 'Admin',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Đăng xuất',
            onPressed: () => _logout(context),
          ),
        ],
      ),
      drawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            DrawerHeader(
              decoration: BoxDecoration(color: Theme.of(context).primaryColor),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  const Icon(
                    Icons.library_books,
                    size: 40,
                    color: Colors.white,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Math IBook',
                    style: Theme.of(
                      context,
                    ).textTheme.titleLarge?.copyWith(color: Colors.white),
                  ),
                  Text(
                    user?.name ?? 'Admin',
                    style: Theme.of(
                      context,
                    ).textTheme.bodySmall?.copyWith(color: Colors.white70),
                  ),
                ],
              ),
            ),
            _drawerItem(context, Icons.dashboard, 'Bảng điều khiển', 0),
            _drawerItem(context, Icons.book, 'Chương học', 1),
            _drawerItem(context, Icons.menu_book, 'Bài học', 2),
            _drawerItem(context, Icons.help, 'Câu hỏi', 3),
            _drawerItem(context, Icons.quiz, 'Bài Quiz', 4),
            _drawerItem(context, Icons.emoji_events, 'Huy hiệu', 5),
            _drawerItem(context, Icons.people, 'Học sinh', 6),
            _drawerItem(context, Icons.policy, 'Chính sách thưởng', 7),
            _drawerItem(context, Icons.notifications, 'Thông báo', 8),
            _drawerItem(context, Icons.settings, 'Cài đặt', 9),
          ],
        ),
      ),
      body: ResponsiveContent(maxWidth: 1200, child: navigationShell),
    );
  }

  Widget _drawerItem(
    BuildContext context,
    IconData icon,
    String label,
    int index,
  ) {
    return ListTile(
      leading: Icon(icon),
      title: Text(label),
      selected: navigationShell.currentIndex == index,
      onTap: () {
        Navigator.pop(context); // Close the drawer
        navigationShell.goBranch(index, initialLocation: true);
      },
    );
  }
}
