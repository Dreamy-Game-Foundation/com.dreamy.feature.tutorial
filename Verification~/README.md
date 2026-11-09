# Kiểm chứng bản 0.2.0

Ngày 2026-11-09, Unity 6000.4.12f1, Linux Editor. Consumer độc lập biên dịch source package/dependency và chạy **33/33 EditMode, 17/17 PlayMode**, không skip. Baseline trước thay đổi: 27/27 EditMode. Fingerprint source, manifest, asmdefs và test C# nằm trong `receipt.json`; commit phát hành được đánh dấu bằng tag local `v0.2.0`. Không có publication/push tự động.

Consumer import sample mới, không dùng script assembly của sandbox. UniTask/DOTween được lấy từ cache đã resolve của host; manifest khai báo toàn bộ built-in modules tiêu chuẩn của host vì DOTween có module audio. Xem dependency versions/commits trong receipt. Sandbox lưu XML và manifest/lock consumer tại `docs/evidence/tutorial-integration/`.

Chạy lại từ sandbox bằng output directory mới:

```bash
python3 LocalPackages/com.dreamy.feature.tutorial/Tests~/verify-consumer.py \
  --unity /path/to/Unity --host . --output /tmp/tutorial-verification-new
```

Script nhận local dependencies từ `LocalPackages`, Unity-generated cache cho UniTask/DOTween và version đã resolve trong packages-lock. Script tạo project mới, không ghi scene/prefab/settings của host. Có thể dùng `--mode EditMode` hoặc `--mode PlayMode`. Kết quả gồm XML, log, command, exit code và source fingerprint. Unity GUI đang mở sandbox không cản consumer riêng.

Kiểm tra đã bao phủ token/save retry, multi-target readiness, invalid config, custom view/factory lifecycle, camera/mask/trigger/occluder/pooling 2D, Physics2DRaycaster, spotlight union area/input separation, waiting timeout/recovery, recovery button lifecycle, drag/hold/release/multi-touch/outside/pause. Test duplicate ID chủ động kỳ vọng error log đó.

Chưa kiểm chứng player/device, card TMP của game và thứ tự render sprite thực tế. Minimum Unity 6000.0 là khai báo package, không phải phạm vi chạy test. Workflow/compatibility registry được AGENTS tham chiếu chưa có trong workspace; receipt này ghi kết quả thực chạy, không tạo registry thay thế. Harness Settings tổng đang bị sample Settings lệch từ trước và không được dùng làm gate phát hành Tutorial.
