# Dreamy Tutorial

Checkpointed, linear tutorials for UI and 3D games. Version 0.1.0.

## Boundaries

- Runtime contains validated DataConfig definitions, Datasave checkpoints, service, and presenter contracts.
- Integration Runtime contains UGUI overlay, scoped registry, Button targets and Collider targets. It has no dependency on Shop, Settings, project code or render-pipeline shaders.
- The host starts flows, resolves message keys, navigates scenes/panels and locks gameplay actions. The overlay only filters UI raycasts; it cannot disable keyboard, gamepad, camera controls or arbitrary gameplay scripts.
- Editor tooling is in a separate Editor-only assembly.

## Bootstrap

Register `TutorialInstaller.RegisterConfig(dataConfig)` before DataConfig initialization. The JSON document key is `tutorialCatalog`; a Resources-based consumer should provide exactly one `Resources/DataConfig/tutorialCatalog.json`.

After initialization, call `TutorialInstaller.Install(catalog, datasave, saveKey)` or construct `TutorialModel` directly for a host-owned service. The installer registers `ITutorialService`; the registering root must unregister it at teardown. A direct model is not globally registered.

Create a `TutorialTargetRegistry`, `TutorialOverlay` and `TutorialController`, then call `controller.Initialize(service, overlay, registry)`. Call `service.TryStart(flowId)` when host UI is ready. The controller polls targets in LateUpdate; pure presenters require their host to call `Tick()`.

`overlay.ResolveMessage` is a host-provided `Func<string, string>`. Sample message keys are readable English text; production keys should be localized by the host.

## Targets

For a Button, add `TutorialUITarget` and assign ID/registry/Button, or use `Configure`. Targets register in OnEnable and unregister in OnDisable. IDs must be unique within a registry. Reconfigure/re-enable does not duplicate the click subscription.

For a 3D object, add `TutorialWorldTarget` on the Collider GameObject and assign an explicit camera, collider and registry. The camera needs `PhysicsRaycaster`, and the scene needs an EventSystem/input module. Click-through requires an exact collider hit using `interactionMask`; another collider in front blocks the interaction. Configure the camera's PhysicsRaycaster mask consistently with that interaction mask. Trigger colliders are supported only when both the event system physics settings and target raycasts agree.

3D bounds are projected into a screen-space rectangular spotlight, independent of URP/Built-in shader choices. Targets behind the camera, crossing near/far planes or outside the viewport wait without advancing. Availability describes projected visibility, not full mesh occlusion; collider raycasts enforce occlusion for click interaction. There is no outline shader or camera steering in v0.1.0.

UI geometry must not be fully clipped by a ScrollRect/mask; the host should scroll it into view. The rectangular spotlight does not describe arbitrary image alpha or nonrectangular buttons.

## Completion and failure

- `Next`: the presenter reports the current token.
- `TargetClick`: the real Button/EventSystem click is observed. No gameplay action is simulated.
- `HostSignal`: capture `service.GetState().StepToken` before the action; after success call `controller.ReportSignal(signalKey, capturedToken)`. Never fetch a fresh token inside an old async callback.
- A signal can complete a step after its previously available target disappears during navigation. A step that has never been active cannot accept such a signal while waiting.
- Skip is policy-controlled and persists separately from completion. Suspend revokes tokens; resume creates a new token.
- Persistence writes a detached candidate before changing in-memory progress. Save failure leaves the same step/token retryable. The overlay Retry button repeats only the failed checkpoint command, not the host gameplay action.
- Host actions must still be idempotent or gated while a checkpoint retry is pending. Closing/disabling the controller loses its in-memory retry command; restarting resumes the saved step, and the host must reconcile an already completed action safely.

Checkpoints use stable step IDs. A removed ID requires `stepMigrations` on its flow, mapping old ID to an existing step ID. Catalog identity changes return `MigrationRequired`; the host must provide an explicit migration strategy. Keep terminal flow IDs stable across catalog revisions.

## Sample

Import **Tutorial Feature** through Package Manager. Open its `Generated/TutorialDemo.unity` scene and press Play (turn off a forced bootstrap start-scene override for this standalone scene), or use **Dreamy > Tutorial > Build UI and 3D Demo** to generate an independent scene and overlay prefab. The builder creates a new folder and preserves existing scenes. Sample Editor assembly requires `com.unity.inputsystem`; the runtime package does not depend on that input implementation.

The sample includes a UI HostSignal flow and a 3D Collider click flow. Reset deletes only `tutorial-demo` in the sample's separate `DreamyTutorialDemo` save directory. It does not reset game/economy saves.

## Verification

See [validation evidence](VALIDATION.md) for results and remaining gates. Platform/device validation and package publication have not been performed.
