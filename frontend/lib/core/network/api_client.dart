import 'package:dio/dio.dart';
import 'app_config.dart';
import '../storage/secure_storage_service.dart';

class ApiClient {
  final Dio _dio;
  final SecureStorageService _storageService;

  ApiClient(this._storageService)
      : _dio = Dio(
          BaseOptions(
            baseUrl: AppConfig.baseUrl,
            connectTimeout: const Duration(seconds: 10),
            receiveTimeout: const Duration(seconds: 10),
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
            },
          ),
        ) {
    _dio.interceptors.add(AuthInterceptor(_storageService));
    _dio.interceptors.add(
      LogInterceptor(
        requestBody: true,
        responseBody: true,
        logPrint: (obj) => print('[API] $obj'),
      ),
    );
  }

  static ApiClient? _instance;
  static ApiClient get instance {
    _instance ??= ApiClient(SecureStorageService());
    return _instance!;
  }

  static void init(SecureStorageService storage) {
    _instance = ApiClient(storage);
  }

  Dio get dio => _dio;

  Future<Response<T>> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) {
    return _dio.get<T>(
      path,
      queryParameters: queryParameters,
      options: options,
      cancelToken: cancelToken,
    );
  }

  Future<Response<T>> post<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) {
    return _dio.post<T>(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
      cancelToken: cancelToken,
    );
  }

  Future<Response<T>> put<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) {
    return _dio.put<T>(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
      cancelToken: cancelToken,
    );
  }

  Future<Response<T>> patch<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) {
    return _dio.patch<T>(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
      cancelToken: cancelToken,
    );
  }

  Future<Response<T>> delete<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) {
    return _dio.delete<T>(
      path,
      data: data,
      queryParameters: queryParameters,
      options: options,
      cancelToken: cancelToken,
    );
  }

  Future<Map<String, dynamic>> login(String email, String password) async {
    final response = await post<Map<String, dynamic>>(
      '/auth/login',
      data: {'email': email, 'password': password},
    );
    return response.data ?? {};
  }

  Future<Map<String, dynamic>> register(String name, String email, String password, String confirmPassword, String otp) async {
    final response = await post<Map<String, dynamic>>(
      '/auth/register',
      data: {'name': name, 'email': email, 'password': password, 'confirmPassword': confirmPassword, 'otp': otp},
    );
    return response.data ?? {};
  }

  Future<void> sendOtp(String email) async {
    await post(
      '/auth/send-otp',
      data: {'email': email},
    );
  }

  Future<void> forgotPassword(String email) async {
    await post(
      '/auth/forgot-password',
      data: {'email': email},
    );
  }

  Future<void> resetPassword(String email, String otp, String newPassword, String confirmNewPassword) async {
    await post(
      '/auth/reset-password',
      data: {
        'email': email,
        'otp': otp,
        'newPassword': newPassword,
        'confirmNewPassword': confirmNewPassword,
      },
    );
  }

  Future<void> logout() async {
    final refreshToken = await _storageService.getRefreshToken();
    if (refreshToken != null) {
      try {
        await post('/auth/logout', data: {'refreshToken': refreshToken});
      } catch (_) {}
    }
  }

  Future<Map<String, dynamic>> refreshToken(String refreshToken) async {
    final response = await post<Map<String, dynamic>>(
      '/auth/refresh',
      data: {'refreshToken': refreshToken},
    );
    return response.data ?? {};
  }

  static String mapDioErrorToMessage(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return 'Connection timed out. Please check your internet connection.';
      case DioExceptionType.badResponse:
        final statusCode = e.response?.statusCode;
        final message = e.response?.data is Map ? e.response?.data['message'] : null;
        if (message is String && message.isNotEmpty) return message;
        switch (statusCode) {
          case 400:
            return 'Invalid request. Please check your input.';
          case 401:
            return 'Session expired. Please login again.';
          case 403:
            return 'Access denied. You do not have permission.';
          case 404:
            return 'Resource not found.';
          case 409:
            return 'Conflict. The resource already exists.';
          case 422:
            return 'Validation error. Please check your input.';
          case 429:
            return 'Too many requests. Please try again later.';
          case 500:
            return 'Server error. Please try again later.';
          default:
            return 'An error occurred (${statusCode ?? 'unknown'}). Please try again.';
        }
      case DioExceptionType.connectionError:
        return 'No internet connection. Please check your network.';
      case DioExceptionType.cancel:
        return 'Request was cancelled.';
      default:
        return 'An unexpected error occurred. Please try again.';
    }
  }
}

class AuthInterceptor extends Interceptor {
  final SecureStorageService _storage;

  AuthInterceptor(this._storage);

  @override
  void onRequest(
      RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await _storage.getAccessToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      if (err.requestOptions.path.contains('/auth/refresh')) {
        await _storage.clearAll();
        handler.reject(DioException(
          requestOptions: err.requestOptions,
          message: 'Session expired',
          error: 'Session expired',
          type: DioExceptionType.badResponse,
          response: err.response,
        ));
        return;
      }

      final refreshToken = await _storage.getRefreshToken();
      if (refreshToken == null || refreshToken.isEmpty) {
        await _storage.clearAll();
        handler.reject(DioException(
          requestOptions: err.requestOptions,
          message: 'Session expired',
          error: 'Session expired',
          type: DioExceptionType.badResponse,
          response: err.response,
        ));
        return;
      }

      try {
        final refreshDio = Dio(BaseOptions(baseUrl: AppConfig.baseUrl));
        final response = await refreshDio.post(
          '/auth/refresh',
          data: {'refreshToken': refreshToken},
        );
        final data = response.data;
        if (data == null || data['accessToken'] == null) {
          throw Exception('Invalid refresh response');
        }
        await _storage.saveAccessToken(data['accessToken'] as String);
        if (data['refreshToken'] != null) {
          await _storage.saveRefreshToken(data['refreshToken'] as String);
        }

        err.requestOptions.headers['Authorization'] =
            'Bearer ${data['accessToken']}';
        final retryDio = Dio(BaseOptions(baseUrl: AppConfig.baseUrl));
        final retryResponse = await retryDio.fetch(err.requestOptions);
        handler.resolve(retryResponse);
      } catch (e) {
        await _storage.clearAll();
        handler.reject(DioException(
          requestOptions: err.requestOptions,
          message: 'Session expired',
          error: 'Session expired',
          type: DioExceptionType.badResponse,
          response: err.response,
        ));
      }
    } else if (err.response?.statusCode == 403) {
      handler.reject(DioException(
        requestOptions: err.requestOptions,
        message: 'Access denied. You do not have permission.',
        error: 'Access denied',
        type: DioExceptionType.badResponse,
        response: err.response,
      ));
    } else {
      handler.next(err);
    }
  }
}
