using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Dreamy.Tutorial.Integration
{
    [RequireComponent(typeof(Collider))]
    public sealed class TutorialWorldTarget : MonoBehaviour, ITutorialScreenTarget, IPointerClickHandler
    {
        [SerializeField] private string targetId;
        [SerializeField] private TutorialTargetRegistry registry;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private LayerMask interactionMask = ~0;
        private bool registered;
        public string Id => targetId;
        public event Action Clicked;
        public bool IsAvailable => registered && isActiveAndEnabled && targetCollider != null && targetCollider.enabled &&
            targetCamera != null && targetCamera.isActiveAndEnabled && TryGetScreenRect(out _);
        public void Configure(string id, TutorialTargetRegistry owner, Camera camera, Collider collider)
        {
            if (registry != null) registry.Unregister(this);
            registered = false; targetId = id; registry = owner; targetCamera = camera; targetCollider = collider;
            if (isActiveAndEnabled) registered = registry != null && registry.Register(this);
        }
        private void Awake() { if (targetCollider == null) targetCollider = GetComponent<Collider>(); }
        private void OnEnable() { registered = registry != null && registry.Register(this); }
        private void OnDisable() { if (registry != null) registry.Unregister(this); registered = false; }
        public bool TryGetScreenRect(out Rect rect)
        {
            rect = default;
            if (!isActiveAndEnabled || targetCollider == null || !targetCollider.enabled || targetCamera == null) return false;
            if ((targetCamera.cullingMask & (1 << targetCollider.gameObject.layer)) == 0) return false;
            Bounds bounds = targetCollider.bounds;
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = targetCamera.WorldToScreenPoint(corner);
                if (point.z <= targetCamera.nearClipPlane || point.z >= targetCamera.farClipPlane) return false;
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.width > 0 && rect.height > 0 && rect.Overlaps(targetCamera.pixelRect);
        }
        public bool ContainsInput(Vector2 screenPoint)
        {
            if (!IsAvailable || !targetCamera.pixelRect.Contains(screenPoint)) return false;
            Ray ray = targetCamera.ScreenPointToRay(screenPoint);
            return Physics.Raycast(ray, out RaycastHit hit, targetCamera.farClipPlane, interactionMask, QueryTriggerInteraction.Collide) &&
                hit.collider == targetCollider;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && ContainsInput(eventData.position)) Clicked?.Invoke();
        }
    }
}
