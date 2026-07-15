# 🤖 AGENTS.md — Hướng dẫn AI Agent tránh Conflict

> **QUAN TRỌNG:** File này dành cho AI coding assistants (Cursor, GitHub Copilot, Antigravity, v.v.).
> Đọc kỹ trước khi chỉnh sửa bất kỳ file nào trong repository này.

---

## 📋 Phân công theo thành viên

### 👤 HIẾU — ADM-01 → ADM-04 (Admin Content Management)

**⛔ KHÔNG ĐƯỢC tự ý sửa các file sau nếu không phải làm việc cho Hiếu:**

#### Flutter Frontend
| File | Mô tả | Trạng thái |
|---|---|---|
| `frontend/lib/features/admin/chapters_admin/admin_chapters_screen.dart` | Quản lý chương (publish/unpublish, taxonomy) | ✅ Hoàn thiện |
| `frontend/lib/features/admin/chapters_admin/admin_lessons_screen.dart` | Quản lý bài học (có nút Quiz) | ✅ Hoàn thiện |
| `frontend/lib/features/admin/lessons_admin/lesson_editor_screen.dart` | Editor bài học (LaTeX + taxonomy) | ✅ Hoàn thiện |
| `frontend/lib/features/admin/quizzes_admin/admin_lesson_quiz_screen.dart` | Xem/quản lý quiz bài học | ✅ Hoàn thiện |
| `frontend/lib/features/admin/quizzes_admin/admin_quiz_editor_screen.dart` | Tạo/sửa quiz bài học | ✅ Hoàn thiện |
| `frontend/lib/features/admin/dashboard/admin_dashboard_screen.dart` | Dashboard admin | ✅ Hoàn thiện |
| `frontend/lib/features/admin/reports/admin_reports_screen.dart` | Báo cáo thống kê | ✅ Hoàn thiện |

#### Models (dùng chung — cẩn thận khi sửa)
| File | Trường do Hiếu thêm | Ghi chú |
|---|---|---|
| `frontend/lib/core/models/chapter_model.dart` | `isPublished`, `curriculumTopicId` | Thêm 2 trường mới — KHÔNG xóa |
| `frontend/lib/core/models/lesson_model.dart` | `curriculumTopicId`, `simulationType → String?` | Đổi type + thêm trường — KHÔNG rollback |
| `frontend/lib/core/models/admin_models.dart` | Class `CurriculumTopicDto`, `AdminQuizDto` (cuối file) | Thêm 2 class ở cuối file — KHÔNG xóa |

#### Backend (Hiếu KHÔNG sửa backend — chỉ dùng API)
Các controller sau do backend team quản lý, Hiếu chỉ gọi API:
- `AdminChaptersController.cs` → `/api/admin/chapters`
- `AdminLessonsController.cs` → `/api/admin/lessons`
- `AdminQuizzesController.cs` → `/api/admin/quizzes`
- `AdminCurriculumTopicsController.cs` → `/api/admin/curriculum-topics`
- `AdminLearningReportsController.cs` → `/api/admin/reports`

---

### 👤 HÙNG — (Cập nhật phạm vi tại đây)

> ⚠️ Hùng hoặc AI agent của Hùng: hãy điền phạm vi file vào đây để các thành viên khác biết.

```
# Ví dụ:
# frontend/lib/features/student/...  → phần của Hùng
# backend/src/.../Controllers/...    → phần của Hùng
```

---

### 👤 HOÀNG — (Cập nhật phạm vi tại đây)

> ⚠️ Hoàng hoặc AI agent của Hoàng: hãy điền phạm vi file vào đây.

```
# Ví dụ:
# frontend/lib/features/...  → phần của Hoàng
```

---

### 👤 TUẤN ANH — (Cập nhật phạm vi tại đây)

> ⚠️ Tuấn Anh hoặc AI agent của Tuấn Anh: hãy điền phạm vi file vào đây.

```
# Ví dụ:
# backend/src/.../Services/...  → phần của Tuấn Anh
```

---

## 🚦 Quy tắc tránh Conflict cho AI Agent

### ✅ BẠN CÓ THỂ làm:
1. Đọc và phân tích bất kỳ file nào để hiểu kiến trúc
2. Sửa file trong phạm vi thành viên bạn đang hỗ trợ
3. Thêm code mới vào file chưa được ai claim

### ⛔ KHÔNG ĐƯỢC làm:
1. **Xóa hoặc đổi tên** các trường đã được thêm bởi thành viên khác trong model files
2. **Rollback** `simulationType` từ `String?` về `String` trong `lesson_model.dart`
3. **Xóa** class `CurriculumTopicDto` hoặc `AdminQuizDto` khỏi `admin_models.dart`
4. **Sửa** `admin_chapters_screen.dart`, `lesson_editor_screen.dart`, `admin_lesson_quiz_screen.dart`, `admin_quiz_editor_screen.dart` nếu không làm việc cho Hiếu
5. **Thay đổi** response format của các Admin API endpoints mà không báo cáo tất cả thành viên

### ⚠️ CẦN CẨN THẬN (shared files):
| File | Vấn đề tiềm ẩn |
|---|---|
| `frontend/lib/core/models/admin_models.dart` | File dùng chung — chỉ THÊM class mới ở cuối, không sửa class của người khác |
| `frontend/lib/core/models/chapter_model.dart` | Đã có thêm field — không xóa `isPublished`, `curriculumTopicId` |
| `frontend/lib/core/models/lesson_model.dart` | `simulationType` đã đổi sang nullable — không rollback |
| `frontend/lib/core/router/app_router.dart` | Router chung — báo cáo nhóm trước khi thêm route |
| `frontend/lib/features/admin/shell/admin_shell.dart` | Navigation chung — không tự ý thêm/xóa nhánh |

---

## 🗂️ Cấu trúc thư mục theo thành viên

```
frontend/lib/features/admin/
├── dashboard/          → HIẾU (ADM-01)
├── chapters_admin/     → HIẾU (ADM-02, ADM-03)
├── lessons_admin/      → HIẾU (ADM-03)
├── quizzes_admin/      → HIẾU (ADM-04) ← THƯ MỤC MỚI
├── questions_admin/    → (Xác nhận với nhóm)
├── badges_admin/       → (Xác nhận với nhóm)
├── users_admin/        → (Xác nhận với nhóm)
├── reports/            → HIẾU (ADM-01)
└── shell/              → CHUNG — cẩn thận khi sửa
```

---

## 📌 API Endpoints đã ổn định (không thay đổi contract)

Các endpoint sau đã được Flutter frontend kết nối — **KHÔNG thay đổi response schema**:

```
GET  /api/admin/curriculum-topics        → CurriculumTopicDto[]
GET  /api/admin/chapters                 → ChapterModel[] (có isPublished, curriculumTopicId)
POST /api/admin/chapters                 → body: { title, description, orderIndex, curriculumTopicId }
PATCH /api/admin/chapters/{id}/publish   → toggle IsPublished
GET  /api/admin/lessons/chapter/{id}     → LessonModel[] (có curriculumTopicId)
POST /api/admin/lessons                  → body: { chapterId, title, contentBody, simulationType, orderIndex, curriculumTopicId }
PATCH /api/admin/lessons/{id}/publish    → toggle IsPublished
GET  /api/admin/quizzes?lessonId=...     → AdminQuizDto[] (quizType là int: 0=Lesson, 1=Chapter)
POST /api/admin/quizzes                  → body: { title, quizType: 0, lessonId, durationSeconds, passScore, firstPassCoins }
PATCH /api/admin/quizzes/{id}/publish    → toggle IsPublished
GET  /api/admin/reports/overview?days=N  → ReportOverviewDto
```

---

## 📝 Lịch sử thay đổi

| Ngày | Thành viên | Thay đổi |
|---|---|---|
| 15/07/2026 | **Hiếu** | Hoàn thiện ADM-01→04: thêm taxonomy dropdown, publish toggle, tạo quiz admin screens |

---

> 💡 **Tip cho AI Agent:** Trước khi sửa bất kỳ file nào, hãy kiểm tra file này để xem file đó có thuộc phạm vi của thành viên khác không. Nếu không chắc, đọc git log hoặc hỏi người dùng.
