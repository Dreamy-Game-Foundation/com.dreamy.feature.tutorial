# Changelog

## [0.2.0] - 2026-11-09

- Bind custom ITutorialView/ITutorialTargetResolver through TutorialController and the presenter host; preserve legacy overloads and serialized fields. Add generic sample factory registration.
- Add Collider2D world target with explicit camera, masks, viewport projection, pooled registration and nearest-hit input. Add a RectTransform target without Button.
- Add optional focusTargetIds/inputTargetIds with legacy targetId fallback, all-target readiness and multi-hole spotlight union rendering. Completion authority remains unchanged.
- Add configurable waiting deadlines, localized waiting messages and separate recovery intent; never skip or advance automatically.
- Add drag/hold/release HostSignal example and EditMode/PlayMode regressions.
- Require Core 1.2.0, DataConfig/Datasave 1.0.1, UI 1.1.0 and Unity physics2d module; document direct UI Presentation references. No save migration or existing asset GUID changes.

## [0.1.1] - 2026-11-06

- Include the sample in-memory tutorial save service so the demo can run without a game save adapter. Real game persistence remains host-owned.
- Synchronize package metadata and README; no asset GUID or game-save migration. Prepared locally; no release tag or publication is implied.

## Historical sample cleanup (included in 0.1.1 and later)

- Remove the sample Editor builder and its Editor assembly; retain the catalog validator.
- Keep the ready-made sample scene/prefab and update sample usage instructions.

## 0.1.0 — 2026-10-01

- Add validated linear tutorial flows, explicit completion modes, tokens and checkpoint persistence.
- Add skip, suspend/resume, explicit step migrations and checkpoint retry without replaying host actions.
- Add scoped UI and 3D target adapters, rectangular spotlight, safe-area tooltip and raycast filtering.
- Add importable UI/3D sample, Editor builder/validator and domain/lifecycle tests.
