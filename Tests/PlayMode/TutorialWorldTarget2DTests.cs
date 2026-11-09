using Dreamy.Tutorial.Integration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialWorldTarget2DTests
    {
        private GameObject root;
        private Camera camera;
        private TutorialTargetRegistry registry;
        private TutorialWorldTarget2D target;
        private BoxCollider2D collider;
        [SetUp] public void Setup()
        {
            root = new GameObject("2D test"); registry = root.AddComponent<TutorialTargetRegistry>();
            var cameraObject = new GameObject("camera"); cameraObject.transform.SetParent(root.transform);
            camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true;
            var child = new GameObject("2D target"); child.transform.SetParent(root.transform, false);
            collider = child.AddComponent<BoxCollider2D>(); target = child.AddComponent<TutorialWorldTarget2D>();
            target.Configure("pickup", registry, camera, collider); Physics2D.SyncTransforms();
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);
        private Vector2 Center => camera.WorldToScreenPoint(collider.transform.position);
        [Test] public void Projection_ClipsToViewport_AndRejectsOffscreenAndDisabledCollider()
        {
            Assert.That(target.IsAvailable, Is.True); Assert.That(target.ContainsInput(Center), Is.True);
            collider.enabled = false; Assert.That(target.IsAvailable, Is.False); collider.enabled = true;
            target.transform.position = new Vector3(100,0,0); Physics2D.SyncTransforms(); Assert.That(target.IsAvailable, Is.False);
            target.transform.position = new Vector3(0,0,-20); Physics2D.SyncTransforms(); Assert.That(target.IsAvailable, Is.False);
            target.transform.position = Vector3.zero; Physics2D.SyncTransforms();
            collider.size = new Vector2(100,100); Physics2D.SyncTransforms();
            Assert.That(target.TryGetScreenRect(out var rect), Is.True);
            Assert.That(rect, Is.EqualTo(camera.pixelRect));
        }
        [Test] public void MasksAndOccluders_ControlInput_WithoutHidingFocus()
        {
            var child = new GameObject("blocker"); child.transform.SetParent(root.transform, false);
            child.transform.position = new Vector3(0,0,-2); child.layer = 2; child.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
            Assert.That(target.IsAvailable, Is.True); Assert.That(target.ContainsInput(Center), Is.False);
            target.InteractionMask = ~(1 << 2); Assert.That(target.ContainsInput(Center), Is.True);
            target.InteractionMask = 0; Assert.That(target.ContainsInput(Center), Is.False);
            camera.cullingMask = 0; Assert.That(target.IsAvailable, Is.False);
        }
        [Test] public void CameraSwapAndPooling_RebindWithoutDuplicateCallbacks()
        {
            int clicks = 0; target.Clicked += () => clicks++;
            for (int i=0; i<3; i++) { target.gameObject.SetActive(false); Assert.That(registry.TryResolve("pickup", out _), Is.False); target.gameObject.SetActive(true); }
            Assert.That(registry.TryResolve("pickup", out var resolved), Is.True); Assert.That(resolved, Is.SameAs(target));
            var events = root.AddComponent<EventSystem>();
            target.OnPointerClick(new PointerEventData(events) { position = Center, button = PointerEventData.InputButton.Left });
            Assert.That(clicks, Is.EqualTo(1));
            var other = new GameObject("other camera"); other.transform.SetParent(root.transform); var next = other.AddComponent<Camera>();
            next.transform.position = new Vector3(100,0,-10); target.TargetCamera = next; Assert.That(target.IsAvailable, Is.False);
            target.TargetCamera = camera; camera.enabled = false; Assert.That(target.IsAvailable, Is.False);
            camera.enabled = true; target.Configure("newPickup", registry, camera, collider);
            Assert.That(registry.TryResolve("pickup", out _), Is.False); Assert.That(target.IsAvailable, Is.True);
            Object.DestroyImmediate(target.gameObject); Assert.That(registry.TryResolve("newPickup", out _), Is.False);
        }
        [Test] public void TriggersFollowGlobalQueryPolicy_AndUnsimulatedBodiesAreUnavailable()
        {
            bool previous = Physics2D.queriesHitTriggers;
            try
            {
                collider.isTrigger = true; Physics2D.queriesHitTriggers = true; Assert.That(target.ContainsInput(Center), Is.True);
                Physics2D.queriesHitTriggers = false; Assert.That(target.ContainsInput(Center), Is.False);
                var body = collider.gameObject.AddComponent<Rigidbody2D>(); body.simulated = false; Assert.That(target.IsAvailable, Is.False);
            }
            finally { Physics2D.queriesHitTriggers = previous; }
        }
        [Test] public void Physics2DRaycaster_ProducesTargetHit_AndPointerClickReportsOnce()
        {
            var events = root.AddComponent<EventSystem>();
            var raycaster = camera.gameObject.AddComponent<Physics2DRaycaster>();
            var data = new PointerEventData(events) { position = Center, button = PointerEventData.InputButton.Left };
            var results = new System.Collections.Generic.List<RaycastResult>();
            raycaster.Raycast(data, results);
            Assert.That(results.Count, Is.GreaterThan(0)); Assert.That(results[0].gameObject, Is.EqualTo(target.gameObject));
            int clicks = 0; target.Clicked += () => clicks++;
            ExecuteEvents.Execute(results[0].gameObject, data, ExecuteEvents.pointerClickHandler);
            Assert.That(clicks, Is.EqualTo(1));
        }
        [Test] public void DuplicateId_DoesNotReplaceOriginal()
        {
            var go = new GameObject("duplicate"); go.transform.SetParent(root.transform); go.AddComponent<BoxCollider2D>(); var other = go.AddComponent<TutorialWorldTarget2D>();
            LogAssert.Expect(LogType.Error, "Duplicate tutorial target 'pickup' in registry '2D test'.");
            other.Configure("pickup", registry, camera, go.GetComponent<Collider2D>());
            Assert.That(other.IsAvailable, Is.False); Assert.That(registry.TryResolve("pickup", out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(target));
        }
    }
}
