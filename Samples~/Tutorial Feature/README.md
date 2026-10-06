# Tutorial Feature

Sample của Dreamy Tutorial. Import từ Window > Package Manager > Dreamy Tutorial > Samples > Import. Unity chép nội dung vào Assets/Samples/Dreamy Tutorial/0.1.0/Tutorial Feature/.

## Cấu trúc và tích hợp

Giữ nguyên folder, .meta, asmdef và reference prefab khi chuyển vào project. Chỉ giữ một bản script/asmdef và một JSON cho mỗi key Resources/DataConfig. Bootstrap config/save/wallet/audio tại GameInstaller trước khi bật UI, theo [README package](../../README.md). Link tương đối này dùng trong source package; sau import, mở README package từ Package Manager.

## Sử dụng và sample

Tạo TutorialTargetRegistry, TutorialOverlay và TutorialController; controller.Initialize(service, overlay, registry), sau đó service.TryStart(flowId) khi target sẵn sàng. TutorialUITarget gắn vào Button; TutorialWorldTarget cần collider/camera rõ ràng, PhysicsRaycaster và EventSystem. HostSignal phải capture StepToken trước action và report token đó sau thành công. Giữ flow/step ID, khai báo migration khi bỏ step; save retry không được thực hiện lại gameplay action. Host xử lý localization và khóa input gameplay; overlay chỉ lọc raycast UI. Sample cần Input System cho EventSystem; mở Generated/TutorialDemo.unity để chạy demo. Tắt ép bootstrap scene khi chạy demo độc lập. Overlay không phải UIPanel.

Assembly Dreamy.Tutorial.Sample.Runtime reference Dreamy.Tutorial.Runtime, Dreamy.Tutorial.Integration.Runtime, Dreamy.Datasave.Runtime, Unity.ugui.

## Addressables cho HUD/overlay

TutorialOverlay không phải UIPanel. Có thể đặt trực tiếp dưới Canvas và bind/initialize như hướng dẫn trên. Nếu cần tải theo yêu cầu:

1. Đưa prefab của game vào Addressables Group, ví dụ UI Widgets.
2. Đặt Address là UI/TutorialOverlay.prefab và khai báo constant tương ứng trong class UIAddress.
3. Dùng AssetLoader.LoadAsync<GameObject>(address), instantiate dưới Canvas rồi Bind hoặc Initialize với service/registry cần thiết.
4. Destroy instance khi kết thúc, chỉ unload cache khi không còn consumer. Build content trước khi thử player.

Luồng này cần Dreamy Assets và UniTask ở game. Không truyền type HUD/overlay vào PanelManager.Show<T>(), vì API đó yêu cầu UIPanel subclass.
