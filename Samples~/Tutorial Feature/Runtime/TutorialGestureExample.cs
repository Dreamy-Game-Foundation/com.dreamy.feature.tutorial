using System;
using Dreamy.Tutorial.Integration;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Dreamy.Tutorial.Sample
{
    /// <summary>Example for a raycastable Image without Button. Game code must additionally validate gameplay success.</summary>
    public sealed class TutorialGestureExample : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private TutorialController controller;
        [SerializeField] private TutorialRectTarget surface;
        [SerializeField] private string signalKey = "gesture.done";
        [SerializeField, Min(0)] private float minimumHoldSeconds;
        [SerializeField, Min(0)] private float minimumDragPixels;
        [SerializeField] private bool allowReleaseOutside;
        private int? pointer;
        private Guid token;
        private Vector2 start;
        private float started;
        public void Configure(TutorialController owner, TutorialRectTarget target, string signal,
            float holdSeconds = 0, float dragPixels = 0, bool releaseOutside = false)
        {
            Cancel(); controller = owner; surface = target; signalKey = signal;
            minimumHoldSeconds = Mathf.Max(0, holdSeconds); minimumDragPixels = Mathf.Max(0, dragPixels);
            allowReleaseOutside = releaseOutside;
        }
        private bool Current => pointer.HasValue && controller != null && controller.isActiveAndEnabled &&
            controller.Service != null && controller.Service.GetState().Status == TutorialStatus.ActiveStep &&
            controller.Service.GetState().StepToken == token && surface != null && surface.IsAvailable;
        private void Update() { if (pointer.HasValue && !Current) Cancel(); }
        public void OnPointerDown(PointerEventData data)
        {
            if (pointer.HasValue && !Current) Cancel();
            if (pointer.HasValue || data.button != PointerEventData.InputButton.Left || controller == null ||
                !controller.isActiveAndEnabled || controller.Service == null || surface == null || !surface.ContainsInput(data.position)) return;
            var state = controller.Service.GetState();
            if (state.Status != TutorialStatus.ActiveStep || state.Step.CompletionMode != TutorialCompletionMode.HostSignal || state.Step.SignalKey != signalKey) return;
            pointer = data.pointerId; token = state.StepToken; start = data.position; started = Time.unscaledTime;
        }
        public void OnDrag(PointerEventData data)
        {
            if (pointer == data.pointerId && !Current) Cancel();
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (pointer != data.pointerId) return;
            bool valid = Current && Time.unscaledTime - started >= minimumHoldSeconds &&
                Vector2.Distance(start, data.position) >= minimumDragPixels &&
                (allowReleaseOutside || surface.ContainsInput(data.position));
            Guid captured = token; Cancel();
            if (valid) controller.ReportSignal(signalKey, captured);
        }
        private void Cancel() { pointer = null; token = Guid.Empty; }
        private void OnDisable() => Cancel();
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    }
}
