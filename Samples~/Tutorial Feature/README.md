# Tutorial Feature

Sample của Dreamy Tutorial. Import từ Window > Package Manager > Dreamy Tutorial > Samples > Import. Unity chép nội dung vào Assets/Samples/Dreamy Tutorial/0.1.0/Tutorial Feature/.

## Cấu trúc và tích hợp

Giữ nguyên folder, .meta, asmdef và reference prefab khi chuyển vào project. Chỉ giữ một bản script/asmdef và một JSON cho mỗi key Resources/DataConfig. Bootstrap config/save/wallet/audio tại GameInstaller trước khi bật UI, theo [README package](../../README.md). Link tương đối này dùng trong source package; sau import, mở README package từ Package Manager.

## Sử dụng và sample

Tạo TutorialTargetRegistry, TutorialOverlay và TutorialController; controller.Initialize(factory, service, overlay, registry), sau đó service.TryStart(flowId) khi target sẵn sàng. TutorialUITarget gắn vào Button; TutorialWorldTarget cần collider/camera rõ ràng, PhysicsRaycaster và EventSystem. HostSignal phải capture StepToken trước action và report token đó sau thành công. Giữ flow/step ID, khai báo migration khi bỏ step; save retry không được thực hiện lại gameplay action. Host xử lý localization và khóa input gameplay; overlay chỉ lọc raycast UI. Sample cần Input System cho EventSystem; mở Generated/TutorialDemo.unity để chạy demo. Tắt ép bootstrap scene khi chạy demo độc lập. Overlay không phải UIPanel.

Assembly Dreamy.Tutorial.Sample.Runtime reference Dreamy.Tutorial.Runtime, Dreamy.Tutorial.Integration.Runtime, Dreamy.Datasave.Runtime, UnityEngine.UI.

## Addressables cho HUD/overlay

TutorialOverlay không phải UIPanel. Có thể đặt trực tiếp dưới Canvas và bind/initialize như hướng dẫn trên. Nếu cần tải theo yêu cầu:

1. Đưa prefab của game vào Addressables Group, ví dụ UI Widgets.
2. Đặt Address là UI/TutorialOverlay.prefab và khai báo constant tương ứng trong class UIAddress.
3. Dùng AssetLoader.LoadAsync<GameObject>(address), instantiate dưới Canvas rồi gọi controller.Initialize(factory, service, overlay, registry).
4. Destroy instance khi kết thúc, chỉ unload cache khi không còn consumer. Build content trước khi thử player.

Luồng này cần Dreamy Assets và UniTask ở game. Không truyền type HUD/overlay vào PanelManager.Show<T>(), vì API đó yêu cầu UIPanel subclass.

## Production integration and presenter lifecycle

The editable integration entry point is `TutorialFeatureInstaller` in Samples~. Runtime `TutorialInstaller` remains available for custom UI; games using the supplied views call only the feature installer. All presenters implement the engine-independent `IPanelPresenter` lifecycle in `Dreamy.UI.Presentation`.

```csharp
TutorialFeatureInstaller.RegisterConfig(dataConfig); // Before dataConfig.InitializeAsync.
// After config/save/wallet readiness, using the same factory as other features:
TutorialFeatureInstaller.Install(factory, config, save, view => registry);
// Or reuse a host-owned service: TutorialFeatureInstaller.Install(factory, service, view => registry);
```

Dependencies in this example belong to the composition root. No installer creates an in-memory wallet/save fallback. Model/service own rewards and checkpoints; views only render state and emit intent. Add direct asmdef references to the integration assembly and Dreamy.UI.Presentation wherever their APIs are used.

TutorialOverlay remains a passive non-panel view. TutorialPresenter implements ITickedPanelPresenter. TutorialController keeps the scene-specific Tick/target suspend/resume ownership and delegates presenter creation/disposal to PanelPresenterHost. Initialize it with `(factory, service, overlay, registry)`; supply the resolver used by that overlay when registering. The legacy Initialize overload uses the same host with an explicit local factory. TutorialDemo is retained as a UI/world gameplay scenario fixture, not a second presenter owner.

In the sandbox, GameInstaller installs the service with the existing foundation-tutorials save key; FoundationTutorialAdapter adds the target registry to the created overlay and binds through the shared factory.

Sandbox validation: `python3 LocalPackages/com.dreamy.feature.settings/Tests~/validate-settings.py --shop --features`. This compiles runtime/integration/sample assemblies against their declared references and runs pure managed model/presenter regressions. Unity scene/coroutine/raycast lifecycle still requires Editor/PlayMode validation.
