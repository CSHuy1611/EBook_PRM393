import 'package:flutter_test/flutter_test.dart';

String? getRedirectPath({
  required bool isLoggedIn,
  required String role,
  required String location,
}) {
  if (!isLoggedIn) {
    if (location == '/splash' || location == '/login' || location == '/register') {
      return null;
    }
    return '/login';
  }

  if (role == 'Admin') {
    if (location.startsWith('/student')) return '/admin/dashboard';
    if (location == '/splash' || location == '/login' || location == '/register' || location == '/') {
      return '/admin/dashboard';
    }
    return null;
  }

  if (role == 'Student') {
    if (location.startsWith('/admin')) return '/student/home';
    if (location == '/splash' || location == '/login' || location == '/register' || location == '/') {
      return '/student/home';
    }
    return null;
  }

  return null;
}

void main() {
  group('Auth redirect logic', () {
    test('unauthenticated user accessing /student/home -> redirect to /login', () {
      expect(
        getRedirectPath(isLoggedIn: false, role: '', location: '/student/home'),
        equals('/login'),
      );
    });

    test('unauthenticated user accessing /admin/dashboard -> redirect to /login', () {
      expect(
        getRedirectPath(isLoggedIn: false, role: '', location: '/admin/dashboard'),
        equals('/login'),
      );
    });

    test('unauthenticated user on /login -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: false, role: '', location: '/login'),
        isNull,
      );
    });

    test('unauthenticated user on /splash -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: false, role: '', location: '/splash'),
        isNull,
      );
    });

    test('unauthenticated user on /register -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: false, role: '', location: '/register'),
        isNull,
      );
    });

    test('admin trying to access /student/home -> redirect to /admin/dashboard', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Admin', location: '/student/home'),
        equals('/admin/dashboard'),
      );
    });

    test('student trying to access /admin/dashboard -> redirect to /student/home', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Student', location: '/admin/dashboard'),
        equals('/student/home'),
      );
    });

    test('student on /student/home -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Student', location: '/student/home'),
        isNull,
      );
    });

    test('admin on /admin/dashboard -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Admin', location: '/admin/dashboard'),
        isNull,
      );
    });

    test('admin on / -> redirect to /admin/dashboard', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Admin', location: '/'),
        equals('/admin/dashboard'),
      );
    });

    test('student on / -> redirect to /student/home', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Student', location: '/'),
        equals('/student/home'),
      );
    });

    test('admin on valid admin route -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Admin', location: '/admin/chapters'),
        isNull,
      );
    });

    test('student on valid student route -> no redirect', () {
      expect(
        getRedirectPath(isLoggedIn: true, role: 'Student', location: '/student/chapters'),
        isNull,
      );
    });
  });
}
