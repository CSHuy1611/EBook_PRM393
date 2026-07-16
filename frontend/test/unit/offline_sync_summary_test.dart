import 'package:flutter_test/flutter_test.dart';

import 'package:math_ibook/core/sync/offline_sync_service.dart';

void main() {
  group('OfflineSyncSummary', () {
    test('coi tiến độ không có quiz là dữ liệu đã đồng bộ', () {
      const summary = OfflineSyncSummary(attempts: 0, progress: 2);

      expect(summary.hasSyncedData, isTrue);
      expect(summary.totalItems, 2);
    });

    test('không báo đồng bộ khi hàng đợi trống', () {
      const summary = OfflineSyncSummary(attempts: 0, progress: 0);

      expect(summary.hasSyncedData, isFalse);
      expect(summary.totalItems, 0);
    });

    test('tính tổng quiz và tiến độ đã đồng bộ', () {
      const summary = OfflineSyncSummary(attempts: 3, progress: 2);

      expect(summary.hasSyncedData, isTrue);
      expect(summary.totalItems, 5);
    });
  });
}
