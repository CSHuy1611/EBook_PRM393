import 'package:dio/dio.dart';
import 'app_config.dart';
import '../storage/secure_storage_service.dart';

class ApiClient {
  // Dio là HTTP client dùng chung cho toàn bộ ứng dụng. Mọi feature gọi qua
  // instance này để dùng cùng base URL, timeout, header và cơ chế refresh JWT.
  final Dio _dio;
  // SecureStorageService giữ access token/refresh token trong vùng lưu trữ an toàn.
  final SecureStorageService _storageService;

  ApiClient(this._storageService)
    : _dio = Dio(
        BaseOptions(
          // AppConfig tự chọn localhost, Android emulator hoặc biến API_BASE_URL.
          baseUrl: AppConfig.baseUrl,
          // Tăng thời gian chờ lên 60 giây để chờ AI sinh câu hỏi
          connectTimeout: const Duration(seconds: 60),
          receiveTimeout: const Duration(seconds: 60),
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
          },
        ),
      ) {
    // AuthInterceptor chạy trước request để gắn JWT và chạy khi có lỗi 401/403.
    _dio.interceptors.add(AuthInterceptor(_storageService));
    // Log request/response phục vụ debug; không tham gia xử lý nghiệp vụ.
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
    // Lazy singleton: chỉ tạo client ở lần truy cập đầu tiên.
    _instance ??= ApiClient(SecureStorageService());
    return _instance!;
  }

  static void init(SecureStorageService storage) {
    // Cho phép bootstrap ứng dụng truyền đúng storage đã được khởi tạo.
    _instance = ApiClient(storage);
  }

  Dio get dio => _dio;

  Future<Response<T>> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
    Options? options,
    CancelToken? cancelToken,
  }) {
    // Giữ nguyên generic T để feature có thể yêu cầu Map, List hoặc DTO khác nhau.
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
    // data là JSON body; queryParameters là tham số trên URL.
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
    // PUT được dùng trong chức năng cập nhật hồ sơ Student.
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

  Future<Map<String, dynamic>> register(
    String name,
    String email,
    String password,
    String confirmPassword,
    String otp,
  ) async {
    final response = await post<Map<String, dynamic>>(
      '/auth/register',
      data: {
        'name': name,
        'email': email,
        'password': password,
        'confirmPassword': confirmPassword,
        'otp': otp,
      },
    );
    return response.data ?? {};
  }

  Future<void> sendOtp(String email) async {
    await post('/auth/send-otp', data: {'email': email});
  }

  Future<void> forgotPassword(String email) async {
    await post('/auth/forgot-password', data: {'email': email});
  }

  Future<void> resetPassword(
    String email,
    String otp,
    String newPassword,
    String confirmNewPassword,
  ) async {
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
        return 'Hết thời gian kết nối. Vui lòng kiểm tra lại mạng Internet.';
      case DioExceptionType.badResponse:
        final statusCode = e.response?.statusCode;
        final data = e.response?.data;
        if (data is Map) {
          if (data.containsKey('isValid') &&
              data['isValid'] == false &&
              data['errors'] is List) {
            final errors = data['errors'] as List;
            if (errors.isNotEmpty && errors.first is Map) {
              return errors.first['message'] ?? 'Dữ liệu không hợp lệ.';
            }
          }
          final message = data['message'] ?? data['detail'] ?? data['Detail'];
          if (message is String && message.isNotEmpty) return message;
          final title = data['title'] ?? data['Title'];
          if (title is String && title.isNotEmpty) return title;
        }
        switch (statusCode) {
          case 400:
            return 'Yêu cầu không hợp lệ. Vui lòng kiểm tra lại.';
          case 401:
            return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
          case 403:
            return 'Từ chối truy cập. Bạn không có quyền thực hiện hành động này.';
          case 404:
            return 'Không tìm thấy dữ liệu.';
          case 409:
            return 'Xung đột dữ liệu. Dữ liệu này đã tồn tại.';
          case 422:
            return 'Lỗi xác thực. Vui lòng kiểm tra lại.';
          case 429:
            return 'Quá nhiều yêu cầu. Vui lòng thử lại sau.';
          case 500:
            return 'Lỗi máy chủ (Server). Vui lòng thử lại sau.';
          default:
            return 'Đã xảy ra lỗi (${statusCode ?? 'không rõ'}). Vui lòng thử lại.';
        }
      case DioExceptionType.connectionError:
        return 'Không có kết nối mạng. Vui lòng kiểm tra lại kết nối và thử lại :((.';
      case DioExceptionType.cancel:
        return 'Yêu cầu đã bị hủy.';
      default:
        return 'Đã xảy ra sự cố không xác định. Vui lòng thử lại.';
    }
  }
}

class AuthInterceptor extends Interceptor {
  final SecureStorageService _storage;

  AuthInterceptor(this._storage);

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Đọc token ngay trước lúc gửi để luôn dùng token mới nhất sau refresh/login.
    final token = await _storage.getAccessToken();
    if (token != null && token.isNotEmpty) {
      // Backend JwtBearer đọc token từ header Authorization chuẩn Bearer.
      options.headers['Authorization'] = 'Bearer $token';
    }
    // Cho request tiếp tục đi qua chuỗi interceptor đến server.
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    // 401 nghĩa là access token thiếu, hết hạn hoặc không hợp lệ.
    if (err.response?.statusCode == 401) {
      // Nếu chính request refresh cũng 401 thì không được refresh đệ quy.
      if (err.requestOptions.path.contains('/auth/refresh')) {
        await _storage.clearAll();
        handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            message: 'Session expired',
            error: 'Session expired',
            type: DioExceptionType.badResponse,
            response: err.response,
          ),
        );
        return;
      }

      final refreshToken = await _storage.getRefreshToken();
      // Không có refresh token thì phiên đăng nhập không thể phục hồi.
      if (refreshToken == null || refreshToken.isEmpty) {
        await _storage.clearAll();
        handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            message: 'Session expired',
            error: 'Session expired',
            type: DioExceptionType.badResponse,
            response: err.response,
          ),
        );
        return;
      }

      bool refreshSuccess = false;
      try {
        // Dùng Dio riêng, không gắn AuthInterceptor, để tránh vòng lặp interceptor.
        final refreshDio = Dio(BaseOptions(baseUrl: AppConfig.baseUrl));
        final response = await refreshDio.post(
          '/auth/refresh',
          data: {'refreshToken': refreshToken},
        );
        final data = response.data;
        // Response refresh bắt buộc phải có accessToken mới.
        if (data == null || data['accessToken'] == null) {
          throw Exception('Invalid refresh response');
        }
        // Lưu token mới trước khi retry để các request tiếp theo dùng được ngay.
        await _storage.saveAccessToken(data['accessToken'] as String);
        if (data['refreshToken'] != null) {
          await _storage.saveRefreshToken(data['refreshToken'] as String);
        }
        
        refreshSuccess = true;
      } catch (e) {
        // Refresh thất bại: xóa credential cũ để tránh lặp 401 vô hạn.
        await _storage.clearAll();
        handler.reject(
          DioException(
            requestOptions: err.requestOptions,
            message: 'Session expired',
            error: 'Session expired',
            type: DioExceptionType.badResponse,
            response: err.response,
          ),
        );
        return;
      }

      if (refreshSuccess) {
        try {
          // Gắn access token mới trực tiếp vào request đã thất bại.
          final newToken = await _storage.getAccessToken();
          err.requestOptions.headers['Authorization'] = 'Bearer $newToken';
          
          // Gửi lại đúng method, URL, body và query của request ban đầu.
          final retryDio = Dio(BaseOptions(baseUrl: AppConfig.baseUrl));
          final retryResponse = await retryDio.fetch(err.requestOptions);
          handler.resolve(retryResponse);
        } catch (e) {
          // Retry thất bại (thường do FormData không thể fetch lại). Không xóa session!
          handler.reject(
            DioException(
              requestOptions: err.requestOptions,
              message: 'Vui lòng thực hiện lại thao tác (Token đã được làm mới).',
              error: e.toString(),
              type: DioExceptionType.unknown,
            ),
          );
        }
      }
    } else if (err.response?.statusCode == 403) {
      // 403 là đã xác thực nhưng role/quyền không cho phép thực hiện request.
      handler.reject(
        DioException(
          requestOptions: err.requestOptions,
          message: 'Access denied. You do not have permission.',
          error: 'Access denied',
          type: DioExceptionType.badResponse,
          response: err.response,
        ),
      );
    } else {
      handler.next(err);
    }
  }
}
