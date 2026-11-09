using System.Collections;
using Dreamy.Tutorial.Integration;
using Dreamy.Tutorial.Sample;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Sample.Tests
{
    public sealed class TutorialGestureExampleTests
    {
        private GameObject root;
        private TutorialGestureExample gesture;
        private TutorialController controller;
        private TutorialModel model;
        private EventSystem events;
        private Vector2 center;
        private sealed class View : ITutorialView
        {
            public event System.Action NextRequested; public event System.Action SkipRequested; public event System.Action RetryRequested;
            public void Render(TutorialState state, ITutorialTarget target) { }
            public void ShowError(string error) { }
            public void Hide() { }
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = new GameObject("Gesture tests", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var registry = root.AddComponent<TutorialTargetRegistry>(); events = root.AddComponent<EventSystem>();
            var child = new GameObject("Gesture Image", typeof(RectTransform), typeof(Image)); child.transform.SetParent(root.transform, false);
            var rect = child.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(200,200);
            var target = child.AddComponent<TutorialRectTarget>(); target.Configure("gesture", registry, rect, canvas);
            controller = root.AddComponent<TutorialController>();
            var catalog = JsonConvert.DeserializeObject<TutorialCatalogConfig>("{\"catalogId\":\"gesture\",\"revision\":1,\"flows\":[{\"id\":\"flow\",\"steps\":[{\"id\":\"drag\",\"messageKey\":\"drag\",\"targetId\":\"gesture\",\"completionMode\":\"HostSignal\",\"signalKey\":\"gesture.done\"}]}]}");
            catalog.Initialize("test"); model = new TutorialModel(catalog, new TutorialDemoMemorySave());
            controller.Initialize(model, new View(), registry); model.TryStart("flow");
            gesture = child.AddComponent<TutorialGestureExample>(); gesture.Configure(controller, target, "gesture.done", 0, 20);
            yield return null; Canvas.ForceUpdateCanvases(); center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }
        private PointerEventData Pointer(int id, Vector2 point) => new(events) { pointerId = id, position = point, button = PointerEventData.InputButton.Left };
        [UnityTest] public IEnumerator DragRelease_UsesHostSignal_AndIgnoresSecondTouch()
        {
            gesture.OnPointerDown(Pointer(0,center)); gesture.OnPointerDown(Pointer(1,center));
            gesture.OnPointerUp(Pointer(1,center + Vector2.right*40)); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            gesture.OnDrag(Pointer(0,center + Vector2.right*40)); gesture.OnPointerUp(Pointer(0,center + Vector2.right*40));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.Completed)); yield return null;
        }
        [UnityTest] public IEnumerator OutsideRelease_AndSuspendDuringDrag_DoNotComplete()
        {
            gesture.OnPointerDown(Pointer(0,center)); gesture.OnPointerUp(Pointer(0,center + Vector2.right*500));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            gesture.OnPointerDown(Pointer(0,center)); controller.enabled = false; controller.enabled = true;
            gesture.OnPointerUp(Pointer(0,center + Vector2.right*40));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep)); yield return null;
        }
        [UnityTest] public IEnumerator ReleaseOnly_Completes_AndApplicationPauseRevokesCapture()
        {
            var target = gesture.GetComponent<TutorialRectTarget>(); gesture.Configure(controller,target,"gesture.done");
            gesture.OnPointerDown(Pointer(0,center));
            gesture.SendMessage("OnApplicationPause", true); controller.SendMessage("OnApplicationPause", true);
            controller.SendMessage("OnApplicationPause", false);
            gesture.OnPointerUp(Pointer(0,center)); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            gesture.OnPointerDown(Pointer(0,center)); gesture.OnPointerUp(Pointer(0,center));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.Completed)); yield return null;
        }
        [UnityTest] public IEnumerator Hold_RequiresDuration_AndDisableCancelsCapture()
        {
            var target = gesture.GetComponent<TutorialRectTarget>(); gesture.Configure(controller,target,"gesture.done",.05f);
            gesture.OnPointerDown(Pointer(0,center)); gesture.OnPointerUp(Pointer(0,center));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            gesture.OnPointerDown(Pointer(0,center)); gesture.enabled = false; gesture.enabled = true;
            yield return new WaitForSecondsRealtime(.06f); gesture.OnPointerUp(Pointer(0,center));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            gesture.OnPointerDown(Pointer(0,center)); yield return new WaitForSecondsRealtime(.06f); gesture.OnPointerUp(Pointer(0,center));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.Completed));
        }
    }
}
