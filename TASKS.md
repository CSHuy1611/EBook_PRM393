# TASKS.md

Trạng thái này được suy ra từ source trên nhánh `tuananh`, không phải backlog product bên ngoài source.

## Done

### Nền tảng backend và frontend

- [x] ASP.NET Core API với EF Core/Npgsql, JWT access/refresh token, role Student/Admin, Swagger Development, exception/active-user middleware.
- [x] Domain entity cho học liệu, quiz/attempt/progress, reward policy, coin transaction, badge/rule/user badge, notification và audit log.
- [x] CRUD/validation/publish/reorder cho phần lớn nội dung và cấu hình admin.
- [x] Flutter auth flow, role redirect, secure token storage, theme, cỡ chữ, thông báo, dashboard, bài học, quiz và simulation UI.
- [x] Unit test backend cho service chính; Flutter unit/widget/integration test scaffold hiện diện.
- [x] Tạo tài liệu `docs/KE_HOACH_TEST_VA_UNIT_TEST_MATH_IBOOK.docx`: kế hoạch Test/Unit Test theo rủi ro cho luồng Student online, offline/retry, Admin publish và các luồng phụ quan trọng; kèm invariant, ma trận test, roadmap, quality gate và checklist release.

### Student STU-10 đến STU-14 trên `tuananh`

- [x] **STU-10 — Xu và lịch sử nhận xu:** `CoinsController`, `StudentFeatureApi`, `CoinsScreen`; có tổng xu, lịch sử nguồn quiz bài/chương/huy hiệu, refresh và phân trang.
- [x] **STU-11 — Bộ sưu tập huy hiệu:** `BadgesController`, `BadgeCheckService`, `BadgesScreen`; status Earned/InProgress/Locked, điều kiện/progress, reconcile trước khi tải UI, reward/notification/idempotency server-side.
- [x] Repair rule badge legacy có ngưỡng thiếu; validation admin buộc ngưỡng dương cho `total_coins`, `passed_quizzes`, `perfect_quiz_streak`.
- [x] **STU-12 — Bảng xếp hạng:** `LeaderboardController`, `LeaderboardScreen`; top 100, current user, highlight, mốc update và refresh.
- [x] **STU-12R — Bảng xếp hạng thành tab chính:** thay tab Bảng tin bằng Xếp hạng, giữ redirect `/student/dashboard`, bổ sung thẻ hạng cá nhân, bục Top 3, danh sách lazy Top 100, avatar fallback, trạng thái loading/error/empty, refresh theo `ProgressNotifier` và 7 widget test.
- [x] Bổ sung 5 tài khoản Student seed idempotent (`student1`–`student5`) với số xu mẫu khác nhau để kiểm tra bảng xếp hạng trên cả database mới và database phát triển hiện có.
- [x] **STU-13 — Hồ sơ:** `ProfileController`/`ProfileService`, `ProfileScreen`; đọc/cập nhật profile, đổi password, thống kê, dark mode, font scale và logout.
- [x] **STU-14 — Offline/sync:** cache SQLite chapter/bài/câu hỏi, queue theo `user_id`, client attempt UUID, sync bulk `/api/sync`, retry/error state và auto-sync khi network event tại StudentShell.

## In Progress

- Không có task product nào được source đánh dấu là in-progress.
- Verification STU-12R ngày 16/07/2026: analyze riêng màn hình/test Leaderboard không có issue; 7/7 widget test Leaderboard đạt; Flutter web build thành công; backend build sạch và 27/27 unit test đạt.
- Verification seed 5 Student ngày 16/07/2026: backend build thành công với 0 warning/error và 27/27 unit test đạt.
- Verification tài liệu test ngày 20/07/2026: backend 27/27 unit test đạt (còn 1 warning `innerEx` chưa dùng); cấu trúc DOCX đạt audit bảng và accessibility. Lần chạy toàn bộ `flutter test` dừng do timeout 180 giây, chưa có kết quả cuối để thay thế baseline cũ.
- Toàn bộ `flutter analyze` còn 97 issue legacy. Toàn bộ `flutter test` đạt 47 test và còn 10 test lỗi cũ ở MathText/Login; cần xử lý riêng và test thủ công Android/Windows trước release.

## Todo

- [ ] Bổ sung test Flutter cho Coins, Badges, Profile, OfflineSyncService và các trạng thái error/empty/loading.
- [ ] Bổ sung test API/integration test cho controller, JWT authorization, pagination và full offline retry/reconcile badge.
- [ ] Xác nhận và tài liệu hoá hỗ trợ SQLite offline trên từng platform. Web hiện được code đánh dấu không hỗ trợ cache offline.
- [ ] Tạo quy trình phát hành/configuration production cho Flutter API URL, JWT secret, PostgreSQL credential và CORS allowlist.
- [ ] Quyết định cách upload/lưu avatar; source hiện chỉ hỗ trợ cập nhật avatar URL.
- [ ] Bổ sung UX để người dùng đi tới Coins trực tiếp từ tất cả vị trí mong muốn nếu product yêu cầu; Leaderboard đã là tab chính và Profile vẫn có liên kết.

## Technical Debt

### Security/configuration

- `appsettings.json` chứa connection string development và JWT key placeholder; cần chuyển secret thực sang environment/secret store, không coi file này là config production.
- CORS policy `AllowFlutterDev` cho phép mọi origin và được dùng không điều kiện; cần restrict cho production.
- `ApiClient` đang luôn gắn `LogInterceptor` log request/response body; `quiz_screen.dart` có `print` body submit. Có rủi ro log dữ liệu nhạy cảm/đáp án và làm nhiễu log release.

### Correctness/robustness

- `ProgressController` bắt mọi exception và trả HTTP 500, gồm cả validation/business error; nên phân biệt `InvalidOperationException`, validation và lỗi hạ tầng.
- `OfflineSyncController` xử lý attempts tuần tự, trong khi một attempt có thể đã ghi xuống DB trước khi item sau lỗi. Retry có `clientAttemptId` giảm duplicate nhưng response hiện chưa có acknowledgement theo từng record/transaction toàn gói.
- `SeedData` chỉ seed nội dung khi database chưa có user; repair ngưỡng rule badge chạy ở startup, nhưng các thay đổi seed nội dung khác không tự áp dụng database cũ.
- Legacy badge condition và structured `BadgeRule` cùng tồn tại; fallback hiện có để sửa dữ liệu cũ nhưng nên có migration/cleanup có kiểm soát để chỉ còn một nguồn rule.

### Performance/maintainability

- Leaderboard tải toàn bộ student vào memory trước khi sort/rank, sau đó mới lấy top 100; cần chuyển ranking/pagination xuống database khi dữ liệu lớn.
- Một số Flutter screen/service chứa logic fetch, parse, state và widget trong cùng file; nên tách repository/view-model khi số feature tăng.
- Local database migration có nhiều SQL string inline và không có automated migration test.
- `README` Flutter cũ trong `frontend/README.md` vẫn là boilerplate từ Flutter; root `README.md` là tài liệu chính hiện tại.
- Chưa thấy TODO/FIXME/HACK trong application source; TODO hiện hữu thuộc template Android/Flutter generated files.

## Next Step

1. Sửa 97 issue `flutter analyze` và 10 test MathText/Login đang lỗi; bổ sung test cho Coins, Badges, Profile và OfflineSyncService trước khi merge/release.
2. Test end-to-end Android: offline quiz -> restart app -> online bulk sync -> xác nhận không duplicate xu/huy hiệu và không regress pass/best score.
3. Chuyển JWT key/database password ra environment/secret store và thay CORS open policy bằng allowlist theo environment.
4. Tắt/giới hạn Dio body logging và bỏ debug `print` trong quiz trước release.
5. Refactor leaderboard để query/rank ở PostgreSQL, kèm test top 100 và người dùng ngoài top.
6. Chuẩn hoá dữ liệu badge legacy sang `BadgeRule` bằng migration/script có backup và test data migration.
7. Cải thiện error contract của progress/offline sync: status code chính xác, per-item result hoặc transaction strategy rõ ràng.
