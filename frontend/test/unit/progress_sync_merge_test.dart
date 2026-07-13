import 'package:flutter_test/flutter_test.dart';
import 'package:math_ibook/core/models/progress_models.dart';

/// Merges client and server progress data.
/// Returns the data that should be used (client wins on ties).
ProgressItemDto mergeProgress(ProgressItemDto client, ProgressResultDto server) {
  final clientTime = DateTime.tryParse(client.clientUpdatedAt) ?? DateTime(2000);
  final serverTime = DateTime.tryParse(server.updatedAt) ?? DateTime(2000);

  if (clientTime.isAfter(serverTime)) return client;
  if (serverTime.isAfter(clientTime)) {
    return ProgressItemDto(
      lessonId: client.lessonId,
      isCompleted: server.isCompleted,
      bestScore: server.bestScore,
      clientUpdatedAt: server.updatedAt,
    );
  }

  if (client.bestScore > server.bestScore) return client;
  if (server.bestScore > client.bestScore) {
    return ProgressItemDto(
      lessonId: client.lessonId,
      isCompleted: server.isCompleted,
      bestScore: server.bestScore,
      clientUpdatedAt: server.updatedAt,
    );
  }

  return client;
}

void main() {
  group('Progress sync merge', () {
    test('client has newer updatedAt -> uses client data', () {
      final client = ProgressItemDto(
        lessonId: 'L1',
        isCompleted: false,
        bestScore: 50.0,
        clientUpdatedAt: '2024-06-15T10:00:00Z',
      );
      final server = ProgressResultDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 80.0,
        updatedAt: '2024-06-14T10:00:00Z',
      );

      final result = mergeProgress(client, server);

      expect(result.bestScore, equals(50.0));
      expect(result.isCompleted, isFalse);
    });

    test('server has newer updatedAt -> uses server data', () {
      final client = ProgressItemDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 90.0,
        clientUpdatedAt: '2024-06-14T10:00:00Z',
      );
      final server = ProgressResultDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 85.0,
        updatedAt: '2024-06-15T10:00:00Z',
      );

      final result = mergeProgress(client, server);

      expect(result.bestScore, equals(85.0));
      expect(result.isCompleted, isTrue);
    });

    test('same updatedAt, client has higher bestScore -> uses client data', () {
      final client = ProgressItemDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 95.0,
        clientUpdatedAt: '2024-06-15T10:00:00Z',
      );
      final server = ProgressResultDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 80.0,
        updatedAt: '2024-06-15T10:00:00Z',
      );

      final result = mergeProgress(client, server);

      expect(result.bestScore, equals(95.0));
      expect(result.isCompleted, isTrue);
    });

    test('same updatedAt, server has higher bestScore -> uses server data', () {
      final client = ProgressItemDto(
        lessonId: 'L1',
        isCompleted: false,
        bestScore: 60.0,
        clientUpdatedAt: '2024-06-15T10:00:00Z',
      );
      final server = ProgressResultDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 90.0,
        updatedAt: '2024-06-15T10:00:00Z',
      );

      final result = mergeProgress(client, server);

      expect(result.bestScore, equals(90.0));
      expect(result.isCompleted, isTrue);
    });

    test('same updatedAt and same bestScore -> no change (client wins tie)', () {
      final client = ProgressItemDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 80.0,
        clientUpdatedAt: '2024-06-15T10:00:00Z',
      );
      final server = ProgressResultDto(
        lessonId: 'L1',
        isCompleted: true,
        bestScore: 80.0,
        updatedAt: '2024-06-15T10:00:00Z',
      );

      final result = mergeProgress(client, server);

      expect(result.bestScore, equals(80.0));
      expect(result.isCompleted, isTrue);
    });
  });
}
