# Dreamy Tutorial

Package thuộc Dreamy Game Studio. Hướng dẫn dưới đây mô tả cấu trúc, cách cài vào project và tích hợp ở root/scene.

## Cài package

Dùng Unity 6000.0 trở lên. Sandbox đã tham chiếu package bằng `file:../LocalPackages/com.dreamy.feature.tutorial`. Project khác dùng Package Manager > + > Install package from disk và chọn package.json, hoặc Git URL của repository nội bộ. Cài cả dependency Dreamy/Git vào manifest của game; version dependency không tự cấu hình registry riêng.

Dependency trực tiếp theo package.json:

- `com.dreamy.core` (1.1.2)
- `com.dreamy.dataconfig` (0.2.0)
- `com.dreamy.datasave` (0.2.0)
- `com.unity.nuget.newtonsoft-json` (3.2.1)
- `com.unity.ugui` (2.0.0)
- `com.unity.modules.physics` (1.0.0)

## Cấu trúc và asmdef

| Assembly | Reference | Phạm vi |
| --- | --- | --- |
| `Dreamy.Tutorial.Editor` | Dreamy.Tutorial.Runtime, Dreamy.Tutorial.Integration.Runtime, Dreamy.DataConfig.Runtime | Chỉ Editor |
| `Dreamy.Tutorial.Integration.Runtime` | Dreamy.Tutorial.Runtime, UnityEngine.UI | Runtime |
| `Dreamy.Tutorial.Runtime` | Dreamy.Core.Runtime, Dreamy.DataConfig.Runtime, Dreamy.Datasave.Runtime | Runtime |

Trong asmdef của game, thêm assembly chứa API trực tiếp sử dụng. Code bootstrap reference thêm Core/DataConfig/Datasave/Economy theo nhu cầu; code async reference UniTask. Code gọi type sample reference assembly sample. Giữ Editor reference trong asmdef Editor-only.

## Cấu trúc và trách nhiệm

Runtime/Config chứa catalog; Contracts chứa service/view và adapter; Domain chứa quy tắc và state; Installation chứa installer; Persistence xử lý tiến trình lưu. Presentation (nếu có) nối service với view. Samples~ là integration được import vào Assets; game sở hữu UI, gameplay, localization và adapter SDK.

## Cài service ở GameInstaller

Dùng một DataConfig và Datasave dùng chung. Ghép đoạn dưới vào root async; không tạo lại các service trong panel.

```csharp
using Dreamy.Core;
using Dreamy.DataConfig;
using Dreamy.Tutorial;

// dataConfig: instance root đã tạo, chưa initialize.
TutorialInstaller.RegisterConfig(dataConfig);
await dataConfig.InitializeAsync(cancellationToken);
ServiceLocator.Register<IDataConfigService>(dataConfig);
// IDatasaveService và wallet (nếu cần) đã đăng ký trước đây.
ITutorialService service = TutorialInstaller.Install();
```

Với nhiều feature, gọi tất cả RegisterConfig trước một InitializeAsync, rồi mới gọi Install cho từng feature. JSON cần có đúng một Resources/DataConfig/tutorialCatalog.json. Root unregister ITutorialService khi teardown; dispose presenter/subscription theo lifecycle UI.

## Sử dụng và sample

Tạo TutorialTargetRegistry, TutorialOverlay và TutorialController; controller.Initialize(factory, service, overlay, registry), sau đó service.TryStart(flowId) khi target sẵn sàng. TutorialUITarget gắn vào Button; TutorialWorldTarget cần collider/camera rõ ràng, PhysicsRaycaster và EventSystem. HostSignal phải capture StepToken trước action và report token đó sau thành công. Giữ flow/step ID, khai báo migration khi bỏ step; save retry không được thực hiện lại gameplay action. Host xử lý localization và khóa input gameplay; overlay chỉ lọc raycast UI. Sample cần Input System cho EventSystem; mở Generated/TutorialDemo.unity để chạy demo. Tắt ép bootstrap scene khi chạy demo độc lập. Overlay không phải UIPanel.

## Import sample

Mở Window > Package Manager, chọn Dreamy Tutorial > Samples > Import. Unity chép vào Assets/Samples/Dreamy Tutorial/0.1.0/. Chuyển cả folder nếu tùy biến, giữ .meta và reference prefab; không giữ bản script/asmdef hoặc Resources document trùng.

- **Tutorial Feature**: nguồn `Samples~/Tutorial Feature`.
  Assembly `Dreamy.Tutorial.Sample.Runtime` reference Dreamy.Tutorial.Runtime, Dreamy.Tutorial.Integration.Runtime, Dreamy.Datasave.Runtime, UnityEngine.UI.

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
