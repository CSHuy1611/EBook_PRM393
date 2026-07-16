import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:math_ibook/features/auth/domain/auth_provider.dart';
import 'package:math_ibook/features/auth/presentation/splash_screen.dart';
import 'package:math_ibook/features/auth/presentation/login_screen.dart';
import 'package:math_ibook/features/auth/presentation/register_screen.dart';
import 'package:math_ibook/features/student/shell/student_shell.dart';
import 'package:math_ibook/features/student/chapters/chapters_screen.dart';
import 'package:math_ibook/features/student/chapters/lessons_screen.dart';
import 'package:math_ibook/features/student/lessons/lesson_detail_screen.dart';
import 'package:math_ibook/features/student/quiz/quiz_screen.dart';
import 'package:math_ibook/features/student/quiz/quiz_result_screen.dart';
import 'package:math_ibook/features/student/quiz/chapter_quiz_screen.dart';
import 'package:math_ibook/features/student/dashboard/dashboard_screen.dart';
import 'package:math_ibook/features/student/badges/badges_screen.dart';
import 'package:math_ibook/features/student/profile/profile_screen.dart';
import 'package:math_ibook/features/student/coins/coins_screen.dart';
import 'package:math_ibook/features/student/leaderboard/leaderboard_screen.dart';
import 'package:math_ibook/features/student/offline_sync/offline_sync_screen.dart';
import 'package:math_ibook/features/student/home/student_home_screen.dart';
import 'package:math_ibook/features/student/notifications/notifications_screen.dart';
import 'package:math_ibook/features/admin/shell/admin_shell.dart';
import 'package:math_ibook/features/admin/dashboard/admin_dashboard_screen.dart';
import 'package:math_ibook/features/admin/chapters_admin/admin_chapters_screen.dart';
import 'package:math_ibook/features/admin/chapters_admin/admin_lessons_screen.dart';
import 'package:math_ibook/features/admin/questions_admin/admin_questions_screen.dart';
import 'package:math_ibook/features/admin/badges_admin/admin_badges_screen.dart';
import 'package:math_ibook/features/admin/users_admin/admin_users_screen.dart';
import 'package:math_ibook/features/admin/users_admin/user_history_screen.dart';
import 'package:math_ibook/features/admin/reports/admin_reports_screen.dart';

final GlobalKey<NavigatorState> _rootNavigator = GlobalKey<NavigatorState>();

GoRouter createAppRouter(AuthProvider authProvider) {
  return GoRouter(
    navigatorKey: _rootNavigator,
    initialLocation: '/splash',
    refreshListenable: authProvider,
    redirect: (BuildContext context, GoRouterState state) {
      final isLoggedIn = authProvider.isAuthenticated;
      final role = authProvider.currentUser?.role ?? '';
      final location = state.uri.toString();

      if (!isLoggedIn) {
        if (location == '/splash' || location == '/login' || location == '/register') return null;
        return '/login';
      }
      if (role == 'Admin') {
        if (location.startsWith('/student')) return '/admin/dashboard';
        if (location == '/splash' || location == '/login' || location == '/register') return '/admin/dashboard';
        if (location == '/') return '/admin/dashboard';
        return null;
      }
      if (role == 'Student') {
        if (location.startsWith('/admin')) return '/student/home';
        if (location == '/splash' || location == '/login' || location == '/register') return '/student/home';
        if (location == '/') return '/student/home';
        return null;
      }
      return null;
    },
    routes: [
      GoRoute(path: '/', redirect: (_, __) => '/splash'),
      GoRoute(path: '/splash', builder: (_, __) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
      GoRoute(path: '/student/notifications', builder: (_, __) => const NotificationsScreen(),
        parentNavigatorKey: _rootNavigator,
      ),
      GoRoute(path: '/student/coins', builder: (_, __) => const CoinsScreen(), parentNavigatorKey: _rootNavigator),
      GoRoute(path: '/student/leaderboard', builder: (_, __) => const LeaderboardScreen(), parentNavigatorKey: _rootNavigator),
      GoRoute(path: '/student/offline-sync', builder: (_, __) => const OfflineSyncScreen(), parentNavigatorKey: _rootNavigator),
      GoRoute(path: '/student/chapter-quiz/:chapterId', builder: (_, state) => ChapterQuizScreen(chapterId: state.pathParameters['chapterId']!), parentNavigatorKey: _rootNavigator),
      StatefulShellRoute.indexedStack(
        builder: (_, __, navigationShell) => StudentShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/student/home', builder: (_, __) => const StudentHomeScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/student/chapters', builder: (_, __) => const ChaptersScreen(),
              routes: [GoRoute(path: ':id', builder: (_, state) => LessonsScreen(chapterId: state.pathParameters['id']!))],
            ),
            GoRoute(path: '/student/lessons/:id', builder: (_, state) => LessonDetailScreen(lessonId: state.pathParameters['id']!)),
            GoRoute(path: '/student/quiz/:lessonId', builder: (_, state) => QuizScreen(lessonId: state.pathParameters['lessonId']!)),
            GoRoute(path: '/student/quiz/result/:attemptId', builder: (_, state) => QuizResultScreen(attemptId: state.pathParameters['attemptId']!)),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/student/dashboard', builder: (_, __) => const DashboardScreen()),
            GoRoute(path: '/student/badges', builder: (_, __) => const BadgesScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/student/profile', builder: (_, __) => const ProfileScreen()),
          ]),
        ],
      ),
      StatefulShellRoute.indexedStack(
        builder: (_, __, navigationShell) => AdminShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/dashboard', builder: (_, __) => const AdminDashboardScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/chapters', builder: (_, __) => const AdminChaptersScreen(),
              routes: [GoRoute(path: ':chapterId/lessons', builder: (_, state) => AdminLessonsScreen(chapterId: state.pathParameters['chapterId']!))],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/lessons', builder: (_, __) => const AdminLessonsScreen(),
              routes: [GoRoute(path: ':lessonId/questions', builder: (_, state) => AdminQuestionsScreen(lessonId: state.pathParameters['lessonId']!))],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/questions', builder: (_, __) => const AdminQuestionsScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/badges', builder: (_, __) => const AdminBadgesScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/users', builder: (_, __) => const AdminUsersScreen(),
              routes: [GoRoute(path: ':id/history', builder: (_, state) => UserHistoryScreen(userId: state.pathParameters['id']!))],
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/admin/reports', builder: (_, __) => const AdminReportsScreen()),
          ]),
        ],
      ),
    ],
  );
}
