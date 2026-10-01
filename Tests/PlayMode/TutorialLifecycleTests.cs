using System.Collections;
using Dreamy.Tutorial.Integration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialLifecycleTests
    {
        [UnityTest] public IEnumerator DisabledAndDestroyedWorldTarget_Unregisters()
        {
            var root = new GameObject("TutorialLifecycle");
            try
            {
                var registry = root.AddComponent<TutorialTargetRegistry>();
                var camera = root.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -8);
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.transform.SetParent(root.transform, true);
                var target = cube.AddComponent<TutorialWorldTarget>(); target.Configure("cube", registry, camera, cube.GetComponent<Collider>());
                Assert.That(registry.TryResolve("cube", out _), Is.True);
                cube.SetActive(false); Assert.That(registry.TryResolve("cube", out _), Is.False);
                cube.SetActive(true); Assert.That(registry.TryResolve("cube", out var resolved), Is.True);
                Assert.That(resolved, Is.SameAs(target));
                Object.Destroy(cube); yield return null; Assert.That(registry.TryResolve("cube", out _), Is.False);
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest] public IEnumerator Spotlight_HasRendererAndBuildsVisibleMesh()
        {
            var root = new GameObject("SpotlightCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var child = new GameObject("Spotlight", typeof(RectTransform)); child.transform.SetParent(root.transform, false);
                var rect = (RectTransform)child.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var spotlight = child.AddComponent<TutorialSpotlight>(); spotlight.color = new Color(0,0,0,.65f);
                spotlight.SetTarget(null, true); yield return null; Canvas.ForceUpdateCanvases();
                var renderer = child.GetComponent<CanvasRenderer>(); Assert.That(renderer, Is.Not.Null);
                var mesh = renderer.GetMesh();
                Assert.That(mesh, Is.Not.Null); Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(spotlight.IsRaycastLocationValid(Vector2.zero, null), Is.True);
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest] public IEnumerator UIButton_ReenableDoesNotDuplicateClickSubscription()
        {
            var root = new GameObject("TutorialUI", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var registry = root.AddComponent<TutorialTargetRegistry>();
                var child = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); child.transform.SetParent(root.transform, false);
                var rect = (RectTransform)child.transform; rect.sizeDelta = new Vector2(200, 100);
                var button = child.GetComponent<Button>(); var target = child.AddComponent<TutorialUITarget>();
                target.Configure("button", registry, button); int clicks = 0; target.Clicked += () => clicks++;
                for (int i = 0; i < 3; i++) { child.SetActive(false); child.SetActive(true); }
                yield return null; Canvas.ForceUpdateCanvases();
                button.onClick.Invoke(); Assert.That(clicks, Is.EqualTo(1));
                child.SetActive(false); Assert.That(registry.TryResolve("button", out _), Is.False);
            }
            finally { Object.Destroy(root); }
        }
    }
}
