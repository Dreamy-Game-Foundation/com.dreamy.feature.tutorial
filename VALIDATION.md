# Validation — Dreamy Tutorial 0.1.0

Date: 2026-10-01. Unity: 6000.4.12f1, Linux Editor. Local package sandbox; no release tag or device build.

## Passed

- Unity compiled Tutorial Runtime, Integration Runtime, Editor validator, imported sample and host Foundation integration. Console reported zero errors after compilation and smoke checks.
- **27/27 EditMode tests**, assembly `Dreamy.Tutorial.Editor.Tests`, job `d7e783c0bbfc43ffb7c483c36235cb01`: config validation, stable checkpoints, migration, terminal states, duplicate/stale tokens, reentrancy, target waiting, navigation after target loss, save failure/retry, presenter cleanup and 3D projection/occlusion.
- **3/3 PlayMode tests**, assembly `Dreamy.Tutorial.PlayMode.Tests`, job `b83a87eb10784cf0ad7c3bdf0ce83b95`: target disable/destroy registration, repeated UI enable without duplicate listeners, and spotlight CanvasRenderer/mesh generation.
- Runtime smoke of imported standalone sample: UI Next -> HostSignal -> Completed; 3D projected collider -> pointer-click handler -> Completed; terminal restart returns AlreadyCompleted.
- EventSystem raycast smoke after renderer repair: spotlight center hit `TutorialCube`, outside hit `Spotlight`. Active rectangular spotlight mesh had 16 vertices. This validates the supplied PhysicsRaycaster and GraphicRaycaster wiring, in addition to domain commands.
- Foundation bootstrap reached Ready and actual tutorial flow ran Welcome -> Add Score -> Open Shop -> Close Shop -> Completed. Foundation panel returned after Shop destruction. Terminal restart returned AlreadyCompleted.
- Game View inspected at 1170 x 2532 portrait. Responsive header and tooltip fit; spotlight shades outside the 3D cube. No physical touch/device or landscape run is claimed.
- Sample assets were created/saved with Unity Editor APIs, then mirrored with their meta GUIDs into Samples~. Runtime and Integration contain no UnityEditor reference; Editor/test assemblies are separate.

## Environment notes and remaining gates

- Dreamy toolbar's forced BootstrapScene initially redirected PlayMode tests. Validation temporarily disabled that start-scene override and restored it afterward. Tests also temporarily changed play/background settings; these were restored.
- The existing FoundationDemoPanel prefab contains one missing script component while its valid FoundationDemoPanel component still runs. This produced a warning during Foundation smoke; the tutorial change does not remove that unrelated component.
- Fresh Unity sample reimport and serialized-reference inspection are performed in this sandbox; this is not a separate clean checkout/device build.
- Outstanding release gates: Android/iOS build/device checks, landscape/safe-area matrix, non-default cameras/viewports, project-specific gameplay input gating, package repo/tag publication and canonical toolkit compatibility registration when that repository is available.
- The host owns idempotent gameplay and scene reconciliation across a crash between action success and tutorial checkpoint save. Retry in the active presenter does not repeat gameplay.
