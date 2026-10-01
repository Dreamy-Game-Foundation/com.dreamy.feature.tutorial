using System;
using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Integration
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TutorialUITarget : MonoBehaviour, ITutorialScreenTarget
    {
        [SerializeField] private string targetId;
        [SerializeField] private TutorialTargetRegistry registry;
        [SerializeField] private Button button;
        private RectTransform rectTransform;
        private Canvas canvas;
        private bool registered;
        private readonly Vector3[] corners = new Vector3[4];
        public string Id => targetId;
        public event Action Clicked;
        public bool IsAvailable => registered && isActiveAndEnabled && canvas != null && canvas.isActiveAndEnabled &&
            (button == null || button.IsActive() && button.IsInteractable()) && TryGetScreenRect(out _);
        public void Configure(string id, TutorialTargetRegistry owner, Button clickButton)
        {
            Unbind(); targetId = id; registry = owner; button = clickButton;
            if (isActiveAndEnabled) Bind();
        }
        private void Awake() { rectTransform = (RectTransform)transform; canvas = GetComponentInParent<Canvas>(); }
        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();
        private void Bind()
        {
            rectTransform = (RectTransform)transform; canvas = GetComponentInParent<Canvas>();
            if (button == null) button = GetComponent<Button>();
            registered = registry != null && registry.Register(this);
            if (registered && button != null) button.onClick.AddListener(OnClick);
        }
        private void Unbind()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            if (registry != null) registry.Unregister(this);
            registered = false;
        }
        private void OnClick() { if (IsAvailable) Clicked?.Invoke(); }
        public bool TryGetScreenRect(out Rect rect)
        {
            rect = default;
            if (!isActiveAndEnabled || rectTransform == null || canvas == null) return false;
            var root = canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            if (root.renderMode != RenderMode.ScreenSpaceOverlay && camera == null) return false;
            rectTransform.GetWorldCorners(corners);
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                if (camera != null && camera.WorldToScreenPoint(corner).z <= camera.nearClipPlane) return false;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.width > 0 && rect.height > 0 && rect.Overlaps(new Rect(0, 0, Screen.width, Screen.height));
        }
        public bool ContainsInput(Vector2 screenPoint)
        {
            if (!IsAvailable) return false;
            var root = canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, camera);
        }
    }
}
