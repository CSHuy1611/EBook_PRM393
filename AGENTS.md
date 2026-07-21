# AGENTS.md

Hướng dẫn cho AI Coding Agent và contributor làm việc trong Math IBook. Đọc file này cùng `README.md` và `TASKS.md` trước khi chỉnh sửa.

## Project overview

| Hạng mục | Thực tế trong source |
|---|---|
| Backend language/framework | C#, ASP.NET Core Web API, `net8.0` |
| Frontend language/framework | Dart, Flutter; Dart constraint `^3.11.5` |
| Database | PostgreSQL; Docker Compose dùng `postgres:16-alpine` |
| ORM | EF Core + Npgsql |
| Backend package manager | NuGet / `dotnet` |
| Frontend package manager | Pub / `flutter pub` |
| Auth | JWT Bearer, refresh token, BCrypt |

## Architecture

### Backend

```text
Controller -> Application service -> IUnitOfWork/IRepository -> AppDbContext -> PostgreSQL
```

- Controller thuộc `MathIBook.Api/Controllers`; chỉ nhận request, authorize và chuyển đổi response/ProblemDetails.
- Business logic thuộc `MathIBook.Application/Services`, được truy cập qua interface trong `Application/Interfaces`.
- `IUnitOfWork` và generic `IRepository<T>` là contract ở Domain; implementation ở Infrastructure.
- Entity/enum ở Domain; DTO ở Application.
- EF mapping, migration và seed ở Infrastructure. `Program.cs` đăng ký DI scope, JWT, CORS, middleware và auto-migrate khi khởi động.

### Flutter

```text
Feature screen -> core model/API service -> ApiClient/Dio -> Web API
```

- UI theo `lib/features/<role>/<feature>/`.
- Shared framework code theo `lib/core/`.
- State nhỏ dùng Provider (`AuthProvider`, `LocalPrefsService`, `ProgressNotifier`).
- Router dùng `GoRouter` và role redirect.
- Storage: secure storage cho token/user, shared preferences cho UI preferences, SQLite queue/cache cho offline.

## Coding convention suy ra từ source

### C#

- Public class/method/property dùng `PascalCase`; local/parameter/private field dùng `camelCase`, private field có prefix `_`.
- Service interface dùng tiền tố `I` và hậu tố `Service`; DTO dùng hậu tố `Dto`; controller dùng hậu tố `Controller`.
- Dùng nullable reference types và `async`/`await` với `Task`/`Task<T>` cho I/O.
- Inject dependency qua constructor; đăng ký implementation trong `Program.cs` với scope phù hợp (services/repository hiện là scoped).
- Controller dùng attribute route, `[Authorize]`/role để phân quyền. Lỗi nghiệp vụ dự kiến trả `ProblemDetails`; middleware xử lý exception chưa bắt được.
- Validate input ở controller/service trước khi ghi database; dùng EF Core constraint/index cho invariant dữ liệu quan trọng.
- Dùng `ILogger<T>` cho service backend; không ghi secret hoặc dữ liệu nhạy cảm vào log.

### Dart/Flutter

- File và thư mục dùng `snake_case`; class/widget dùng `PascalCase`; field/local dùng `camelCase`.
- Import package nội bộ theo `package:math_ibook/...`; đặt Flutter/Dart import trước các import nội bộ.
- Tái sử dụng `ApiClient`, model core và widget loading/error; không tạo Dio client mới trong mỗi screen nếu không có lý do rõ ràng.
- Với request async, kiểm tra `mounted` trước `setState` sau `await`.
- Route mới phải được khai báo ở `core/router/app_router.dart` và tuân thủ redirect role hiện có.
- Không lưu access/refresh token vào shared preferences hoặc SQLite.
- Offline attempt phải giữ nguyên `clientAttemptId`; chỉ mark synced khi API bulk sync trả thành công.

## Commands

Chạy từ thư mục `EBook_PRM393/` trừ khi có ghi khác.

```powershell
# Docker development
docker compose up --build

# Backend
cd backend
dotnet restore
dotnet run --project src/MathIBook.Api
dotnet build MathIBook.sln
dotnet test MathIBook.sln

# Frontend
cd frontend
flutter pub get
flutter run -d windows
flutter run -d emulator-5554
flutter analyze
flutter test
dart format lib test integration_test
flutter build apk --release
flutter build web
flutter build windows
```

Chạy Flutter với endpoint khác:

```powershell
flutter run --dart-define=API_BASE_URL=http://host:port/api
```

## AI rules

1. Đọc `README.md`, file này và `TASKS.md` trước khi sửa code.
2. Không sửa migration đã tồn tại. Tạo migration mới khi schema EF Core thay đổi và chỉ khi task yêu cầu schema change.
3. Không phá vỡ Domain -> Application -> Infrastructure -> API hoặc cấu trúc `core/features` của Flutter.
4. Chỉ sửa file liên quan trực tiếp đến task; giữ nguyên thay đổi không liên quan trong worktree.
5. Không đổi route API, DTO public, role authorization hoặc format payload nếu không có yêu cầu tương thích rõ ràng.
6. Không xóa code đang hoạt động, dữ liệu seed hoặc test hiện có nếu chưa được yêu cầu.
7. Không hard-code secret, token, password production hoặc URL deployment. Dùng configuration/environment variable.
8. Không log password, token, đáp án đúng hoặc body nhạy cảm; bỏ `print`/debug log tạo mới trước khi hoàn thành.
9. Với reward/badge/offline sync, bảo toàn idempotency, best score và pass state; không cộng xu/huy hiệu ở client.
10. Nếu thay đổi kiến trúc, API contract, environment setup hoặc workflow build/test, cập nhật `README.md` và `AGENTS.md`.
11. Luôn cập nhật `TASKS.md` sau khi hoàn thành task; không đánh dấu Done nếu chưa kiểm tra tương ứng.
12. Không chạy lệnh destructive như reset/delete database, hoặc tự reset worktree, khi chưa có yêu cầu rõ ràng.

## Checklist trước khi hoàn thành

- [ ] Đã đọc `README.md`, `AGENTS.md`, `TASKS.md`.
- [ ] Chỉ thay đổi file trong phạm vi task.
- [ ] Đã kiểm tra compile/build phù hợp: `dotnet build` và/hoặc Flutter build.
- [ ] Đã chạy lint: `flutter analyze` khi sửa Flutter.
- [ ] Đã chạy test liên quan: `dotnet test` và/hoặc `flutter test`.
- [ ] Đã không để lại TODO/FIXME/debug print mới hoặc secret mới.
- [ ] Đã kiểm tra route, authorization, validation và error path khi thay đổi API.
- [ ] Đã cập nhật `TASKS.md`; cập nhật README/AGENTS nếu cấu trúc hoặc workflow đổi.
