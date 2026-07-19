import 'dart:convert';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:path/path.dart' as p;
import 'package:sqflite/sqflite.dart';

/// Persistent cache and per-student sync queue. Scores, rewards and badges
/// remain server-authoritative; this database stores only the learner's inputs.
class LocalDbService {
  // Singleton bảo đảm mọi màn hình dùng cùng một connection SQLite.
  static final LocalDbService _instance = LocalDbService._internal();
  factory LocalDbService() => _instance;
  LocalDbService._internal();

  Database? _db;
  bool _isWeb = false;
  bool get isAvailable => !_isWeb;

  Future<void> init() async {
    // sqflite trong dự án chưa hỗ trợ Web; Web bỏ qua cache thay vì làm crash startup.
    if (kIsWeb) {
      _isWeb = true;
      return;
    }
    // ??= chỉ mở database một lần trong vòng đời ứng dụng.
    _db ??= await _initDb();
  }

  Future<Database> get database async {
    // Mọi thao tác DB đi qua getter để chắc chắn init đã chạy.
    await init();
    if (_isWeb)
      throw UnsupportedError(
        'Offline cache is currently available on mobile/desktop only.',
      );
    return _db!;
  }

  Future<Database> _initDb() async {
    // getDatabasesPath trả thư mục riêng phù hợp Android/iOS/desktop.
    final dbPath = await getDatabasesPath();
    // version=3 kích hoạt _onUpgrade cho thiết bị đã cài phiên bản cũ.
    return openDatabase(
      p.join(dbPath, 'math_ibook.db'),
      version: 3,
      onCreate: _onCreate,
      onUpgrade: _onUpgrade,
    );
  }

  Future<void> _onCreate(Database db, int version) async {
    // Ba bảng đầu là cache nội dung để đọc khi mất mạng.
    await db.execute(
      'CREATE TABLE local_chapters (id TEXT PRIMARY KEY, title TEXT NOT NULL, description TEXT, order_index INTEGER NOT NULL DEFAULT 0, lesson_count INTEGER NOT NULL DEFAULT 0, cached_at TEXT NOT NULL)',
    );
    await db.execute(
      'CREATE TABLE local_lessons (id TEXT PRIMARY KEY, chapter_id TEXT NOT NULL, title TEXT NOT NULL, content_body TEXT, simulation_type TEXT, content_version INTEGER NOT NULL DEFAULT 1, order_index INTEGER NOT NULL DEFAULT 0, cached_at TEXT NOT NULL)',
    );
    await db.execute(
      'CREATE TABLE local_questions (id TEXT PRIMARY KEY, lesson_id TEXT NOT NULL, question_text TEXT NOT NULL, options TEXT NOT NULL, order_index INTEGER NOT NULL DEFAULT 0)',
    );
    // local_progress là queue theo user+lesson; UNIQUE giúp upsert thay vì tạo nhiều dòng.
    await db.execute(
      'CREATE TABLE local_progress (id INTEGER PRIMARY KEY AUTOINCREMENT, user_id TEXT NOT NULL, lesson_id TEXT NOT NULL, client_updated_at TEXT NOT NULL, is_synced INTEGER NOT NULL DEFAULT 0, UNIQUE(user_id, lesson_id))',
    );
    // client_attempt_id UNIQUE là định danh ổn định cho cùng một lần làm quiz khi retry.
    await db.execute(
      'CREATE TABLE local_quiz_attempts (id INTEGER PRIMARY KEY AUTOINCREMENT, user_id TEXT NOT NULL, lesson_id TEXT NOT NULL, quiz_id TEXT, client_attempt_id TEXT NOT NULL UNIQUE, duration_seconds INTEGER NOT NULL DEFAULT 0, answers_json TEXT NOT NULL, client_created_at TEXT NOT NULL, sync_status TEXT NOT NULL DEFAULT \'pending\', retry_count INTEGER NOT NULL DEFAULT 0, last_sync_error TEXT)',
    );
    // Index theo user và trạng thái làm truy vấn queue pending nhanh hơn.
    await db.execute(
      'CREATE INDEX idx_progress_pending ON local_progress(user_id, is_synced)',
    );
    await db.execute(
      'CREATE INDEX idx_attempt_pending ON local_quiz_attempts(user_id, sync_status)',
    );
  }

  Future<void> _onUpgrade(Database db, int oldVersion, int newVersion) async {
    if (oldVersion < 2) {
      await db.execute(
        "ALTER TABLE local_progress ADD COLUMN user_id TEXT NOT NULL DEFAULT ''",
      );
      await db.execute(
        "ALTER TABLE local_quiz_attempts ADD COLUMN user_id TEXT NOT NULL DEFAULT ''",
      );
      await db.execute(
        'ALTER TABLE local_quiz_attempts ADD COLUMN quiz_id TEXT',
      );
      await db.execute(
        "ALTER TABLE local_quiz_attempts ADD COLUMN client_attempt_id TEXT NOT NULL DEFAULT ''",
      );
      await db.execute(
        "ALTER TABLE local_quiz_attempts ADD COLUMN answers_json TEXT NOT NULL DEFAULT '[]'",
      );
      await db.execute(
        "ALTER TABLE local_quiz_attempts ADD COLUMN sync_status TEXT NOT NULL DEFAULT 'pending'",
      );
      await db.execute(
        'ALTER TABLE local_quiz_attempts ADD COLUMN retry_count INTEGER NOT NULL DEFAULT 0',
      );
      await db.execute(
        'ALTER TABLE local_quiz_attempts ADD COLUMN last_sync_error TEXT',
      );
      await db.execute(
        'ALTER TABLE local_lessons ADD COLUMN content_version INTEGER NOT NULL DEFAULT 1',
      );
      await db.execute(
        "ALTER TABLE local_lessons ADD COLUMN cached_at TEXT NOT NULL DEFAULT ''",
      );
      await db.execute(
        'CREATE INDEX IF NOT EXISTS idx_progress_pending ON local_progress(user_id, is_synced)',
      );
      await db.execute(
        'CREATE INDEX IF NOT EXISTS idx_attempt_pending ON local_quiz_attempts(user_id, sync_status)',
      );
    }
    if (oldVersion < 3) {
      await db.execute(
        'CREATE TABLE IF NOT EXISTS local_chapters (id TEXT PRIMARY KEY, title TEXT NOT NULL, description TEXT, order_index INTEGER NOT NULL DEFAULT 0, lesson_count INTEGER NOT NULL DEFAULT 0, cached_at TEXT NOT NULL)',
      );
    }
  }

  Future<void> cacheChapters(List<Map<String, dynamic>> chapters) async {
    if (!isAvailable) return;
    // Batch giảm số lần ghi transaction khi cache nhiều chương.
    final batch = (await database).batch();
    for (final chapter in chapters) {
      // replace cập nhật cache nếu chapter id đã tồn tại.
      batch.insert('local_chapters', {
        'id': chapter['id'],
        'title': chapter['title'] ?? '',
        'description': chapter['description'] ?? '',
        'order_index': chapter['orderIndex'] ?? 0,
        'lesson_count': chapter['lessonCount'] ?? 0,
        'cached_at': DateTime.now().toUtc().toIso8601String(),
      }, conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<List<Map<String, dynamic>>> getCachedChapters() async {
    if (!isAvailable) return [];
    final rows = await (await database).query(
      'local_chapters',
      orderBy: 'order_index',
    );
    return rows
        .map(
          (row) => <String, dynamic>{
            'id': row['id'],
            'title': row['title'],
            'description': row['description'],
            'orderIndex': row['order_index'],
            'lessonCount': row['lesson_count'],
            'completionPercentage': 0,
          },
        )
        .toList();
  }

  Future<void> cacheLesson(Map<String, dynamic> lesson) async {
    if (!isAvailable) return;
    final db = await database;
    await db.insert('local_lessons', {
      'id': lesson['id'],
      'chapter_id': lesson['chapterId'] ?? '',
      'title': lesson['title'] ?? '',
      'content_body': lesson['contentBody'] ?? '',
      'simulation_type': lesson['simulationType'] ?? '',
      'content_version': lesson['contentVersion'] ?? 1,
      'order_index': lesson['orderIndex'] ?? 0,
      'cached_at': DateTime.now().toUtc().toIso8601String(),
    }, conflictAlgorithm: ConflictAlgorithm.replace);
  }

  Future<Map<String, dynamic>?> getCachedLesson(String lessonId) async {
    if (!isAvailable) return null;
    final rows = await (await database).query(
      'local_lessons',
      where: 'id = ?',
      whereArgs: [lessonId],
    );
    return rows.isEmpty ? null : rows.first;
  }

  Future<void> cacheQuestions(
    String lessonId,
    List<Map<String, dynamic>> questions,
  ) async {
    if (!isAvailable) return;
    final db = await database;
    final batch = db.batch();
    // Xóa bộ câu hỏi cũ của lesson trước khi ghi phiên bản mới để tránh câu dư.
    batch.delete(
      'local_questions',
      where: 'lesson_id = ?',
      whereArgs: [lessonId],
    );
    for (final question in questions) {
      batch.insert('local_questions', {
        'id': question['id'],
        'lesson_id': lessonId,
        'question_text': question['questionText'] ?? '',
        // List options được encode JSON vì SQLite không có kiểu List.
        'options': jsonEncode(question['options'] ?? []),
        'order_index': question['orderIndex'] ?? 0,
      }, conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<List<Map<String, dynamic>>> getCachedQuestions(String lessonId) async {
    if (!isAvailable) return [];
    return (await database).query(
      'local_questions',
      where: 'lesson_id = ?',
      whereArgs: [lessonId],
      orderBy: 'order_index',
    );
  }

  Future<Map<String, dynamic>?> getCachedLessonDto(String lessonId) async {
    final lesson = await getCachedLesson(lessonId);
    if (lesson == null) return null;
    final questions = await getCachedQuestions(lessonId);
    return {
      'id': lesson['id'],
      'chapterId': lesson['chapter_id'],
      'title': lesson['title'],
      'contentBody': lesson['content_body'],
      'simulationType': lesson['simulation_type'],
      'orderIndex': lesson['order_index'],
      'isPublished': true,
      'questions': questions
          .map(
            (question) => {
              'id': question['id'],
              'lessonId': question['lesson_id'],
              'questionText': question['question_text'],
              'options': jsonDecode(question['options'] as String),
              'orderIndex': question['order_index'],
            },
          )
          .toList(),
    };
  }

  Future<void> upsertProgress({
    required String userId,
    required String lessonId,
    required DateTime updatedAt,
  }) async {
    if (!isAvailable) return;
    final db = await database;
    // Mỗi lần xem lại bài đặt is_synced=0 để lần có mạng tiếp theo gửi mốc mới.
    final value = {
      'user_id': userId,
      'lesson_id': lessonId,
      'client_updated_at': updatedAt.toUtc().toIso8601String(),
      'is_synced': 0,
    };
    // Tìm theo khóa nghiệp vụ user+lesson; insert lần đầu, update các lần sau.
    final existing = await db.query(
      'local_progress',
      columns: ['id'],
      where: 'user_id = ? AND lesson_id = ?',
      whereArgs: [userId, lessonId],
      limit: 1,
    );
    if (existing.isEmpty) {
      await db.insert('local_progress', value);
    } else {
      await db.update(
        'local_progress',
        value,
        where: 'id = ?',
        whereArgs: [existing.first['id']],
      );
    }
  }

  Future<void> queueQuizAttempt({
    required String userId,
    required String lessonId,
    required String? quizId,
    required String clientAttemptId,
    required int durationSeconds,
    required List<Map<String, dynamic>> answers,
    required DateTime createdAt,
  }) async {
    if (!isAvailable) return;
    // Lưu input của người học, không lưu điểm/xu/badge tự tính ở client.
    await (await database).insert('local_quiz_attempts', {
      'user_id': userId,
      'lesson_id': lessonId,
      'quiz_id': quizId,
      'client_attempt_id': clientAttemptId,
      // answers phải giữ nguyên để backend là nơi chấm điểm khi có mạng.
      'duration_seconds': durationSeconds, 'answers_json': jsonEncode(answers),
      'client_created_at': createdAt.toUtc().toIso8601String(),
      'sync_status': 'pending',
      'retry_count': 0,
      // ignore làm thao tác idempotent nếu cùng clientAttemptId bị queue lại.
    }, conflictAlgorithm: ConflictAlgorithm.ignore);
  }

  Future<List<Map<String, dynamic>>> getUnsyncedProgress(String userId) async {
    if (!isAvailable) return [];
    // Luôn lọc userId để hai tài khoản trên cùng thiết bị không đồng bộ nhầm nhau.
    return (await database).query(
      'local_progress',
      where: 'user_id = ? AND is_synced = 0',
      whereArgs: [userId],
    );
  }

  Future<List<Map<String, dynamic>>> getUnsyncedAttempts(String userId) async {
    if (!isAvailable) return [];
    // failed được đặt lại pending nên mọi trạng thái khác synced đều được retry.
    return (await database).query(
      'local_quiz_attempts',
      where: 'user_id = ? AND sync_status != ?',
      whereArgs: [userId, 'synced'],
      orderBy: 'client_created_at',
    );
  }

  Future<void> markProgressSynced(List<int> ids) async {
    if (!isAvailable || ids.isEmpty) return;
    // Chỉ gọi sau khi POST /sync trả thành công.
    final batch = (await database).batch();
    for (final id in ids) {
      batch.update(
        'local_progress',
        {'is_synced': 1},
        where: 'id = ?',
        whereArgs: [id],
      );
    }
    await batch.commit(noResult: true);
  }

  Future<void> markAttemptsSynced(List<int> ids) async {
    if (!isAvailable || ids.isEmpty) return;
    final batch = (await database).batch();
    for (final id in ids) {
      batch.update(
        'local_quiz_attempts',
        {'sync_status': 'synced', 'last_sync_error': null},
        where: 'id = ?',
        whereArgs: [id],
      );
    }
    await batch.commit(noResult: true);
  }

  Future<void> markAttemptsFailed(List<int> ids, String error) async {
    if (!isAvailable || ids.isEmpty) return;
    // Giữ record để retry, tăng retry_count và lưu lỗi gần nhất cho màn hình.
    final db = await database;
    final batch = db.batch();
    for (final id in ids) {
      batch.rawUpdate(
        'UPDATE local_quiz_attempts SET sync_status = ?, last_sync_error = ?, retry_count = retry_count + 1 WHERE id = ?',
        ['pending', error, id],
      );
    }
    await batch.commit(noResult: true);
  }
}
