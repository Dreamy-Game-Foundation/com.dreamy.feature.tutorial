# Tutorial Feature sample

Requires the Dreamy Tutorial package, Unity UGUI and the Unity Input System package for the supplied Editor builder/EventSystem module.

Open `Generated/TutorialDemo.unity` and press Play. In a Dreamy consumer that forces a bootstrap start scene, turn that override off in the Start Scene toolbar while running this standalone sample. Use **Start UI** for a Button/HostSignal flow and **Start 3D** for a Collider target. Only one flow can run at a time. **Reset tutorial save** clears this sample's tutorial checkpoint, leaving game saves untouched.

To rebuild an independent demo, use **Dreamy > Tutorial > Build UI and 3D Demo**. Output is in a unique `Assets/DreamyTutorialDemo*` folder. The builder creates and closes its own additive scene; it does not save existing scenes. Exit Play Mode and finish unsaved untitled scenes before using it.

The sample uses a screen-space overlay and collider bounds for a rectangular 3D spotlight. It intentionally leaves camera controls, gameplay input gating and localization to the host. The host action increments a demo counter; save retries do not execute that action again.

Runtime integration components live in the package's `Integration/Runtime` assembly and may be used with a customized project-owned prefab.
