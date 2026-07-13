# Math IBook

Ứng dụng học toán tương tác với giải pháp học tập toàn diện.

## Công nghệ sử dụng

- **Backend**: ASP.NET Core 8 Web API, Entity Framework Core, Npgsql (PostgreSQL)
- **Frontend**: Flutter 3.41.9 / Dart 3.11.5 (Android, Web, Windows)
- **Database**: PostgreSQL 18.4

## Cấu trúc dự án

```
backend/
├── MathIBook.sln
├── src/
│   ├── MathIBook.Api/          # API controllers, middleware, Program.cs
│   ├── MathIBook.Application/  # Services, DTOs, interfaces
│   ├── MathIBook.Domain/       # Entities, enums
│   └── MathIBook.Infrastructure/ # DbContext, repositories, migrations
└── tests/
    └── MathIBook.UnitTests/    # Unit tests (xUnit + Moq + FluentAssertions)

frontend/
├── lib/
│   ├── core/              # Network, storage, theme, router, math widgets
│   ├── features/
│   │   ├── student/       # StudentShell + 10 screens
│   │   └── admin/         # AdminShell + 9 screens
│   └── main.dart
├── test/                  # Unit tests + widget tests
├── integration_test/      # Integration tests
└── build/                 # Release artifacts
    ├── app/outputs/flutter-apk/app-release.apk
    └── web/
```

## Hướng dẫn chạy

### Yêu cầu

- .NET 8.0 SDK
- Flutter 3.41.9 / Dart 3.11.5
- PostgreSQL 18.4

### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/MathIBook.Api
```

Server chạy tại `http://localhost:5000`.

### Frontend

```bash
cd frontend
flutter pub get
flutter run -d chrome --web-port=5173    # Web
flutter run -d emulator-5554             # Android
flutter build apk --release              # Build APK
```

## API Endpoints

### Student

| Endpoint | Phương thức | Mô tả |
|---|---|---|
| `/api/auth/login` | POST | Đăng nhập |
| `/api/auth/refresh` | POST | Refresh token |
| `/api/chapters` | GET | Danh sách chương |
| `/api/chapters/{id}/lessons` | GET | Danh sách bài học |
| `/api/lessons/{id}` | GET | Chi tiết bài học |
| `/api/lessons/{id}/questions` | GET | Câu hỏi của bài học |
| `/api/quiz-attempts` | POST | Nộp bài kiểm tra |
| `/api/quiz-attempts` | GET | Lịch sử làm bài |
| `/api/progress/sync` | POST | Đồng bộ tiến trình |
| `/api/dashboard` | GET | Dashboard tổng quan |
| `/api/badges` | GET | Danh sách huy hiệu |

### Admin

| Endpoint | Phương thức | Mô tả |
|---|---|---|
| `/api/admin/chapters` | CRUD | Quản lý chương |
| `/api/admin/chapters/{id}/lessons` | CRUD | Quản lý bài học |
| `/api/admin/chapters/{id}/lessons/{lid}/questions` | CRUD | Quản lý câu hỏi |
| `/api/admin/badges` | CRUD | Quản lý huy hiệu |
| `/api/admin/users` | GET | Quản lý người dùng |
| `/api/admin/reports` | GET | Báo cáo thống kê |

## Tài khoản mẫu

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@mathibook.vn | Admin@123 |
| Student | student@mathibook.vn | Student@123 |

## Kết quả kiểm thử

### Backend Tests
- `dotnet test`: 1/1 passed
- Backend login: Admin (200, role: Admin) + Student (200, role: Student)

### Frontend Build
| Platform | Build | Ghi chú |
|---|---|---|
| Web | ✅ Release build thành công | `build/web/` |
| Android APK | ✅ Release build thành công (62.1 MB) | `app-release.apk` |
| Windows | ❌ Cần bật Developer Mode | Symlink requirement |

### Frontend Tests
- `flutter analyze`: 0 errors
- `flutter test`: 33 unit/working tests passed, widget tests có rendering issues trong test env (complex math widgets)
