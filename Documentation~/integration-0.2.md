# Tích hợp Tutorial 0.2

## Custom view và lifecycle

View của game triển khai `ITutorialView`: render state, phát Next/Skip/Retry intent và hide. TMP, hai icon, hình quỹ đạo và localization nằm trong game. Pack không thêm dependency TMP. Giữ component controller trên object còn hoạt động khi chỉ ẩn nội dung view; không đặt controller dưới content mà `Hide()` tắt.

Đường bind trực tiếp nhận cả biến có kiểu interface:

```csharp
ITutorialView view = gameTutorialCard;
controller.Initialize(service, view, registry, waitingOptions);
service.TryStart("practice");
```

Đường dùng factory chung đăng ký **concrete type** trước khi initialize:

```csharp
factory.Register<GameTutorialCard>(view => new TutorialPresenter(service, view, registry, waitingOptions));
controller.Initialize(factory, service, gameTutorialCard, registry);
// Sample installer có helper tương đương:
// TutorialFeatureInstaller.Install<GameTutorialCard>(factory, service, _ => registry, waitingOptions);
```

Factory nhận type chính xác, không tự tìm implementation của interface. Controller báo lỗi khi factory không tạo `TutorialPresenter`. Factory chịu trách nhiệm cung cấp cùng service/resolver đã bind; không đăng ký trùng type. Overload `TutorialOverlay` cũ vẫn giữ nguyên. Enable/reopen tạo presenter mới; disable/pause dispose presenter và suspend service; resume tạo token mới. Controller đã sở hữu tick và presenter: game không tạo presenter thứ hai.

## Target world 2D và vùng UI không có Button

Gắn `TutorialWorldTarget2D` cạnh `Collider2D`; gọi `Configure(id, registry, camera, collider)`. Có thể đổi `TargetCamera` và `InteractionMask` lúc runtime. Không dùng `Camera.main` ngầm. Gắn `Physics2DRaycaster` vào camera và có EventSystem/input module của game. Đồng bộ interaction mask và raycaster event mask. Collider target phải nằm trong mask; collider vật cản cũng phải nằm trong mask nếu muốn chặn click.

Adapter đăng ký khi enable, hủy khi disable/destroy, configure lại sẽ bỏ ID cũ. Collider bị tắt, Rigidbody2D không simulated, camera tắt, layer không được camera render, target sau camera/ngoài clipping hoặc ngoài viewport sẽ không khả dụng. Target nằm một phần trên màn hình trả rect được cắt theo viewport. Projection hỗ trợ orthographic/perspective, dùng bounds collider nên spotlight là hình chữ nhật bao, không phải silhouette sprite.

Input query dùng physics scene của collider, chấp nhận hit gần nhất đúng collider. Trigger tuân theo `Physics2D.queriesHitTriggers`, giống raycaster; adapter không đổi cấu hình toàn cục. Vật cản chặn input nhưng không tự làm mất focus. Sprite sorting/material transparency không được dùng để suy ra che khuất; nếu cần semantics khác, game cung cấp `ITutorialScreenTarget` riêng. Collider đồng phẳng có thể không phản ánh thứ tự sprite; tách depth hoặc layer/mask phù hợp.

`TutorialRectTarget` là adapter cho `RectTransform` dưới Canvas, không cần `Button`. Camera lấy từ root Canvas (`null` cho ScreenSpaceOverlay). Dùng một `Image` có raycastTarget trên vùng nhận gesture. CanvasGroup/UI occlusion và khóa gameplay input vẫn do host sở hữu.

## Nhiều focus và input độc lập

```json
{
  "id": "practice",
  "messageKey": "tutorial.practice",
  "completionMode": "HostSignal",
  "signalKey": "pickup.impact.confirmed",
  "focusTargetIds": ["joystick", "pickup"],
  "inputTargetIds": ["joystick"],
  "blockOutsideTarget": true
}
```

`targetId` vẫn là đích duy nhất được chấp nhận cho `TargetClick`. Nếu bỏ hai danh sách mới, mỗi danh sách mặc định về `targetId`; `[]` có nghĩa là không có target trong tập đó. ID trong từng tập phải không rỗng và không trùng. Block input cần tập input không rỗng; với TargetClick bị block, tập input phải chứa `targetId`.

Tất cả target trong focus/input và `targetId` (nếu có) đều là bắt buộc: thiếu một target thì chờ, không tự advance. Marker phụ/tùy chọn nên do game render, không đưa vào danh sách bắt buộc. Đây là điều kiện sẵn sàng hiển thị/tương tác; completion vẫn là Next/TargetClick/HostSignal, không có chuỗi AND gameplay trong pack.

`TutorialOverlay` hỗ trợ sẵn nhiều lỗ hình chữ nhật; phần giao nhau không bị phủ tối hai lần. Raycast chỉ đi qua tập input, độc lập tập focus. Chỉ dùng một số ít focus mỗi bước để giữ mesh UI nhỏ. Custom view muốn dùng nhiều target triển khai thêm `ITutorialFocusView.RenderTargets`; danh sách được mượn đến tick kế tiếp, cần copy nếu giữ lại. View chỉ triển khai contract cũ tiếp tục nhận completion target cũ qua `Render`.

## Waiting recovery

```csharp
var waitingOptions = new TutorialWaitingOptions
{
    TimeoutSeconds = 8,
    TargetTimedOut = state => ShowRecoveryHint(state.StepToken),
    RecoverRequested = state => RebuildPracticeFixture(state.StepToken)
};
overlay.ResolveMessage = localization.Resolve;
overlay.WaitingMessageKey = "tutorial.waiting";
overlay.WaitingTimeoutMessageKey = "tutorial.waiting.timeout";
overlay.ConfigureRecovery(recoveryButton);
controller.Initialize(service, (ITutorialView)overlay, registry, waitingOptions);
```

Timeout mặc định bằng 0 (tắt). Clock mặc định monotonic, không phụ thuộc timeScale; host có thể inject clock tính bằng giây. Callback timeout phát một lần trong mỗi lần chờ; target phục hồi, đổi token, suspend hoặc dispose sẽ kết thúc lần chờ đó. Không tự retry/skip/advance. `ITutorialWaitingView` là capability tùy chọn cho custom view; recovery intent tách biệt `RetryRequested` vốn chỉ retry save thất bại. Mỗi nút recovery và save retry phải là control riêng.

Game sở hữu việc dựng lại fixture, cho retry/skip theo policy và cancellation của thao tác async; capture token, kiểm tra token còn hiện hành trước khi áp dụng kết quả. Pack dùng Tick để theo dõi timeout, không thêm UniTask hoặc tác vụ nền. Skip vẫn theo `AllowSkip` của flow.

## Gesture sample

Import sample và xem `TutorialGestureExample`, `Tests/TutorialGestureExampleTests.cs`. Gắn component lên Image không Button cạnh `TutorialRectTarget`, rồi gọi `Configure(controller, target, signalKey, holdSeconds, dragPixels, releaseOutside)`:

- Release: holdSeconds = 0, dragPixels = 0.
- Drag: đặt dragPixels > 0; dùng khoảng cách từ điểm bắt đầu tới điểm thả, tính bằng screen pixel.
- Hold: đặt holdSeconds > 0, dragPixels = 0; dùng unscaled time.
- Kết hợp: cả hai ngưỡng phải đạt. Mặc định thả ngoài vùng sẽ hủy; `releaseOutside = true` cho phép.

Capture một pointer và StepToken tại pointer-down, bỏ qua pointer thứ hai. Disable, pause, mất target, suspend/đổi token sẽ hủy capture. Thả hợp lệ chỉ report HostSignal; save retry không chạy lại gesture. Trong game thật, chỉ report sau khi gameplay xác nhận thành công; không copy tín hiệu demo thay cho kiểm chứng pickup/impact.

Game sở hữu spawn trận tập, AI, damage, buff/debuff, ShotId, điều kiện pickup → impact và khóa gameplay input. Spotlight chỉ lọc UI raycast, không ngăn polling Input System ở gameplay.

## Migration và kiểm chứng

Không đổi save schema, flow/step ID hay meta GUID cũ. Catalog cũ giữ fallback một target. Nếu tác giả đổi/bỏ step ID thì vẫn cần `stepMigrations`. Bản 0.2.0 tăng minor vì thêm public contracts/schema và nâng minimum dependency.

Baseline được chạy trên Unity 6000.4.12f1 với Core 1.2.0, DataConfig/Datasave 1.0.1 và UI 1.1.0. `unity: 6000.0` là minimum khai báo, không phải bằng chứng đã chạy mọi bản Unity 6. Xem receipt/fingerprint phát hành; runtime trên thiết bị và render theo sprite sorting cần game kiểm chứng thêm.
