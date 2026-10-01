using Dreamy.Tutorial.Integration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialTargetTests
    {
        private GameObject root;
        private Camera camera;
        private TutorialTargetRegistry registry;
        [SetUp] public void Setup()
        {
            root = new GameObject("TutorialTest"); registry = root.AddComponent<TutorialTargetRegistry>();
            var go = new GameObject("Camera"); go.transform.SetParent(root.transform); camera = go.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -8);
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);
        private TutorialWorldTarget Cube()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.transform.SetParent(root.transform);
            var target = cube.AddComponent<TutorialWorldTarget>(); target.Configure("cube", registry, camera, cube.GetComponent<Collider>());
            Physics.SyncTransforms(); return target;
        }
        [Test] public void WorldBounds_ProjectAndDisappearBehindCamera()
        {
            var target = Cube(); Assert.That(target.IsAvailable, Is.True);
            Assert.That(target.TryGetScreenRect(out Rect rect), Is.True); Assert.That(rect.width, Is.GreaterThan(0));
            target.transform.position = new Vector3(0, 0, -20); Physics.SyncTransforms(); Assert.That(target.IsAvailable, Is.False);
        }
        [Test] public void WorldClick_RequiresExactCollider_AndRespectsOccluder()
        {
            var target = Cube(); Vector2 point = camera.WorldToScreenPoint(target.transform.position);
            Assert.That(target.ContainsInput(point), Is.True);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.SetParent(root.transform);
            blocker.transform.position = new Vector3(0, 0, -4); Physics.SyncTransforms();
            Assert.That(target.ContainsInput(point), Is.False);
        }
        [Test] public void DuplicateTarget_IsRejectedWithoutReplacingOriginal()
        {
            var first = Cube(); LogAssert.Expect(LogType.Error, "Duplicate tutorial target 'cube' in registry 'TutorialTest'.");
            var second = Cube(); Assert.That(second.IsAvailable, Is.False);
            Assert.That(registry.TryResolve("cube", out var resolved), Is.True); Assert.That(resolved, Is.SameAs(first));
        }
    }
}
