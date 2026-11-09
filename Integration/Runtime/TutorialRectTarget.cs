using System;
using UnityEngine;
namespace Dreamy.Tutorial.Integration
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TutorialRectTarget : MonoBehaviour, ITutorialScreenTarget
    {
        [SerializeField] private string targetId;
        [SerializeField] private TutorialTargetRegistry registry;
        [SerializeField] private RectTransform area;
        [SerializeField] private Canvas ownerCanvas;
        private bool registered;
        public string Id => targetId;
        // Gesture completion uses HostSignal, never a synthesized target click.
        public event Action Clicked { add { } remove { } }
        private Camera EventCamera => ownerCanvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : ownerCanvas.rootCanvas.worldCamera;
        public bool IsAvailable => registered && isActiveAndEnabled && area != null && area.gameObject.activeInHierarchy &&
            ownerCanvas != null && ownerCanvas.isActiveAndEnabled &&
            (ownerCanvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay || EventCamera != null && EventCamera.isActiveAndEnabled) &&
            TryGetScreenRect(out _);
        public void Configure(string id, TutorialTargetRegistry owner, RectTransform rect, Canvas canvas)
        {
            if (registry != null) registry.Unregister(this);
            registered = false; targetId = id; registry = owner; area = rect; ownerCanvas = canvas;
            if (isActiveAndEnabled) registered = registry != null && registry.Register(this);
        }
        private void Awake() { if (area == null) area = GetComponent<RectTransform>(); if (ownerCanvas == null) ownerCanvas = GetComponentInParent<Canvas>(); }
        private void OnEnable() { registered = registry != null && registry.Register(this); }
        private void OnDisable() { if (registry != null) registry.Unregister(this); registered = false; }
        private readonly Vector3[] corners = new Vector3[4];
        public bool TryGetScreenRect(out Rect rect)
        {
            rect = default;
            if (!isActiveAndEnabled || area == null || ownerCanvas == null || !ownerCanvas.isActiveAndEnabled) return false;
            if (ownerCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && (EventCamera == null || !EventCamera.isActiveAndEnabled)) return false;
            area.GetWorldCorners(corners);
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                if (EventCamera != null && EventCamera.WorldToScreenPoint(corner).z <= EventCamera.nearClipPlane) return false;
                var point = RectTransformUtility.WorldToScreenPoint(EventCamera, corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Rect viewport = EventCamera == null ? new Rect(0, 0, Screen.width, Screen.height) : EventCamera.pixelRect;
            return rect.width > 0 && rect.height > 0 && rect.Overlaps(viewport);
        }
        public bool ContainsInput(Vector2 point) => IsAvailable && RectTransformUtility.RectangleContainsScreenPoint(area, point, EventCamera);
    }
}
