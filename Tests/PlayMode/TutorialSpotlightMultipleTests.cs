using System;
using System.Collections;
using Dreamy.Tutorial.Integration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialSpotlightMultipleTests
    {
        private sealed class Target : ITutorialScreenTarget
        {
            public Rect Rect;
            public string Id => "focus";
            public bool IsAvailable => true;
            public event Action Clicked { add { } remove { } }
            public bool TryGetScreenRect(out Rect rect) { rect = Rect; return true; }
            public bool ContainsInput(Vector2 point) => Rect.Contains(point);
        }
        [UnityTest] public IEnumerator WaitingRecoveryButton_RemainsActiveAcrossRenders()
        {
            var root = new GameObject("Recovery overlay");
            try
            {
                var overlay = root.AddComponent<TutorialOverlay>();
                var body = new GameObject("Content"); body.transform.SetParent(root.transform);
                var buttonObject = new GameObject("Recovery", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                buttonObject.transform.SetParent(body.transform);
                var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                overlay.Configure(body, null, null, null, null, null, null); overlay.ConfigureRecovery(button);
                var state = new TutorialState("flow", null, TutorialStatus.WaitingForTarget, Guid.NewGuid(), true);
                overlay.RenderWaiting(state, true); Assert.That(buttonObject.activeSelf, Is.True);
                overlay.RenderTargets(state, null, Array.Empty<ITutorialTarget>(), Array.Empty<ITutorialTarget>());
                Assert.That(buttonObject.activeSelf, Is.True, "Waiting render must preserve pointer capture on recovery button.");
                int intents = 0; overlay.RecoveryRequested += () => intents++;
                for (int i = 0; i < 3; i++) { overlay.enabled = false; overlay.enabled = true; }
                button.onClick.Invoke(); Assert.That(intents, Is.EqualTo(1));
                yield return null;
            }
            finally { UnityEngine.Object.Destroy(root); }
        }
        [UnityTest] public IEnumerator OverlappingHoles_HaveUnionArea_AndInputTargetsAreIndependent()
        {
            var root = new GameObject("Multi Spotlight", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var child = new GameObject("Spotlight", typeof(RectTransform)); child.transform.SetParent(root.transform, false);
                var rect = child.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero; var spotlight = child.AddComponent<TutorialSpotlight>();
                yield return null; Canvas.ForceUpdateCanvases();
                float w = Screen.width, h = Screen.height;
                var a = new Target { Rect = new Rect(.2f*w,.2f*h,.3f*w,.3f*h) };
                var b = new Target { Rect = new Rect(.4f*w,.4f*h,.3f*w,.3f*h) };
                var input = new Target { Rect = new Rect(.75f*w,.75f*h,.1f*w,.1f*h) };
                spotlight.SetTargets(new ITutorialTarget[] { a,b }, new ITutorialTarget[] { input }, true);
                Canvas.ForceUpdateCanvases();
                var mesh = child.GetComponent<CanvasRenderer>().GetMesh(); var vertices = mesh.vertices; var triangles = mesh.triangles;
                float area = 0;
                for (int i=0;i<triangles.Length;i+=3)
                    area += Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]], vertices[triangles[i+2]]-vertices[triangles[i]]).magnitude*.5f;
                float expected = rect.rect.width*rect.rect.height*.83f;
                Assert.That(area, Is.EqualTo(expected).Within(expected*.001f));
                Assert.That(spotlight.IsRaycastLocationValid(a.Rect.center,null), Is.True);
                Assert.That(spotlight.IsRaycastLocationValid(input.Rect.center,null), Is.False);
                spotlight.SetTargets(null,null,false); Assert.That(spotlight.IsRaycastLocationValid(Vector2.zero,null), Is.False);
            }
            finally { UnityEngine.Object.Destroy(root); }
        }
    }
}
