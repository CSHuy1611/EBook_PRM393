import 'dart:convert';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart' as p;

class LocalDbService {
  static final LocalDbService _instance = LocalDbService._internal();
  factory LocalDbService() => _instance;
  LocalDbService._internal();

  Database? _db;
  bool _isWeb = false;

  Future<void> init() async {
    if (kIsWeb) {
      _isWeb = true;
      return;
    }
    _db ??= await _initDb();
  }

  Future<Database> get database async {
    return _db ?? (await _initDb());
  }

  Future<Database> _initDb() async {
    final dbPath = await getDatabasesPath();
    final path = p.join(dbPath, 'math_ibook.db');
    return await openDatabase(
      path,
      version: 1,
      onCreate: _onCreate,
    );
  }

  Future<void> _onCreate(Database db, int version) async {
    await db.execute('''
      CREATE TABLE local_lessons (
        id TEXT PRIMARY KEY,
        chapter_id TEXT NOT NULL,
        title TEXT NOT NULL,
        content_body TEXT,
        simulation_type TEXT,
        order_index INTEGER NOT NULL DEFAULT 0,
        is_published INTEGER NOT NULL DEFAULT 1
      )
    ''');
    await db.execute('''
      CREATE TABLE local_questions (
        id TEXT PRIMARY KEY,
        lesson_id TEXT NOT NULL,
        question_text TEXT NOT NULL,
        options TEXT,
        correct_option INTEGER,
        explanation TEXT,
        order_index INTEGER NOT NULL DEFAULT 0
      )
    ''');
    await db.execute('''
      CREATE TABLE local_progress (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        lesson_id TEXT NOT NULL,
        is_completed INTEGER NOT NULL DEFAULT 0,
        best_score REAL NOT NULL DEFAULT 0.0,
        client_updated_at TEXT NOT NULL,
        is_synced INTEGER NOT NULL DEFAULT 0
      )
    ''');
    await db.execute('''
      CREATE TABLE local_quiz_attempts (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        lesson_id TEXT NOT NULL,
        score REAL NOT NULL DEFAULT 0.0,
        total_questions INTEGER NOT NULL DEFAULT 0,
        duration_seconds INTEGER NOT NULL DEFAULT 0,
        answers TEXT,
        client_created_at TEXT NOT NULL,
        is_synced INTEGER NOT NULL DEFAULT 0
      )
    ''');
  }

  // --- local_lessons ---

  bool get _available => !_isWeb;

  Future<int> insertLesson(Map<String, dynamic> lesson) async {
    if (!_available) return 0;
    final db = await database;
    return await db.insert('local_lessons', lesson,
        conflictAlgorithm: ConflictAlgorithm.replace);
  }

  Future<int> updateLesson(Map<String, dynamic> lesson) async {
    if (!_available) return 0;
    final db = await database;
    return await db.update(
      'local_lessons',
      lesson,
      where: 'id = ?',
      whereArgs: [lesson['id']],
    );
  }

  Future<Map<String, dynamic>?> getLesson(String id) async {
    if (!_available) return null;
    final db = await database;
    final results = await db.query(
      'local_lessons',
      where: 'id = ?',
      whereArgs: [id],
    );
    return results.isNotEmpty ? results.first : null;
  }

  Future<List<Map<String, dynamic>>> getCachedLessonsByChapter(
      String chapterId) async {
    if (!_available) return [];
    final db = await database;
    return await db.query(
      'local_lessons',
      where: 'chapter_id = ?',
      whereArgs: [chapterId],
      orderBy: 'order_index ASC',
    );
  }

  Future<void> deleteLessonsByChapter(String chapterId) async {
    if (!_available) return;
    final db = await database;
    await db.delete(
      'local_lessons',
      where: 'chapter_id = ?',
      whereArgs: [chapterId],
    );
  }

  // --- local_questions ---

  Future<int> insertQuestion(Map<String, dynamic> question) async {
    if (!_available) return 0;
    final db = await database;
    return await db.insert('local_questions', question,
        conflictAlgorithm: ConflictAlgorithm.replace);
  }

  Future<List<Map<String, dynamic>>> getCachedQuestionsByLesson(
      String lessonId) async {
    if (!_available) return [];
    final db = await database;
    return await db.query(
      'local_questions',
      where: 'lesson_id = ?',
      whereArgs: [lessonId],
      orderBy: 'order_index ASC',
    );
  }

  Future<void> deleteQuestionsByLesson(String lessonId) async {
    if (!_available) return;
    final db = await database;
    await db.delete(
      'local_questions',
      where: 'lesson_id = ?',
      whereArgs: [lessonId],
    );
  }

  // --- local_progress ---

  Future<int> insertOrUpdateProgress(Map<String, dynamic> progress) async {
    if (!_available) return 0;
    final db = await database;
    final existing = await db.query(
      'local_progress',
      where: 'lesson_id = ?',
      whereArgs: [progress['lesson_id']],
    );
    if (existing.isNotEmpty) {
      return await db.update(
        'local_progress',
        progress,
        where: 'lesson_id = ?',
        whereArgs: [progress['lesson_id']],
      );
    }
    return await db.insert('local_progress', progress);
  }

  Future<Map<String, dynamic>?> getProgressByLesson(String lessonId) async {
    if (!_available) return null;
    final db = await database;
    final results = await db.query(
      'local_progress',
      where: 'lesson_id = ?',
      whereArgs: [lessonId],
    );
    return results.isNotEmpty ? results.first : null;
  }

  Future<List<Map<String, dynamic>>> getUnsyncedProgress() async {
    if (!_available) return [];
    final db = await database;
    return await db.query(
      'local_progress',
      where: 'is_synced = ?',
      whereArgs: [0],
    );
  }

  Future<void> markProgressSynced(int id) async {
    if (!_available) return;
    final db = await database;
    await db.update(
      'local_progress',
      {'is_synced': 1},
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  // --- local_quiz_attempts ---

  Future<int> insertQuizAttempt(Map<String, dynamic> attempt) async {
    if (!_available) return 0;
    final db = await database;
    return await db.insert('local_quiz_attempts', attempt);
  }

  Future<List<Map<String, dynamic>>> getQuizAttemptsByLesson(
      String lessonId) async {
    if (!_available) return [];
    final db = await database;
    return await db.query(
      'local_quiz_attempts',
      where: 'lesson_id = ?',
      whereArgs: [lessonId],
      orderBy: 'client_created_at DESC',
    );
  }

  Future<List<Map<String, dynamic>>> getUnsyncedAttempts() async {
    if (!_available) return [];
    final db = await database;
    return await db.query(
      'local_quiz_attempts',
      where: 'is_synced = ?',
      whereArgs: [0],
    );
  }

  Future<void> markAttemptSynced(int id) async {
    if (!_available) return;
    final db = await database;
    await db.update(
      'local_quiz_attempts',
      {'is_synced': 1},
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  // --- generic ---

  Future<void> markAsSynced(int id, String table) async {
    if (!_available) return;
    final db = await database;
    await db.update(
      table,
      {'is_synced': 1},
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  Future<void> batchInsertLessons(
      List<Map<String, dynamic>> lessons) async {
    if (!_available) return;
    final db = await database;
    final batch = db.batch();
    for (final lesson in lessons) {
      batch.insert('local_lessons', lesson,
          conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<void> batchInsertQuestions(
      List<Map<String, dynamic>> questions) async {
    if (!_available) return;
    final db = await database;
    final batch = db.batch();
    for (final question in questions) {
      batch.insert('local_questions', question,
          conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }
}
