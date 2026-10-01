using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Integration
{
    // Four quads form a rectangular cutout without a render-pipeline-specific shader.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialSpotlight : MaskableGraphic, ICanvasRaycastFilter
    {
        private Rect screenHole;
        private bool hasHole;
        private bool blocking;
        private ITutorialScreenTarget target;
        public void SetTarget(ITutorialScreenTarget value, bool block)
        {
            target = value; blocking = block;
            hasHole = value != null && value.IsAvailable && value.TryGetScreenRect(out screenHole);
            SetVerticesDirty();
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
            blocking && !(target != null && target.ContainsInput(screenPoint));
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); Rect outer = rectTransform.rect;
            if (!hasHole) { Quad(helper, outer); return; }
            var camera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenHole.min, camera, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenHole.max, camera, out Vector2 max);
            min = Vector2.Max(outer.min, Vector2.Min(outer.max, min));
            max = Vector2.Max(min, Vector2.Min(outer.max, max));
            Quad(helper, Rect.MinMaxRect(outer.xMin, outer.yMin, min.x, outer.yMax));
            Quad(helper, Rect.MinMaxRect(max.x, outer.yMin, outer.xMax, outer.yMax));
            Quad(helper, Rect.MinMaxRect(min.x, outer.yMin, max.x, min.y));
            Quad(helper, Rect.MinMaxRect(min.x, max.y, max.x, outer.yMax));
        }
        private void Quad(VertexHelper helper, Rect rect)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            int start = helper.currentVertCount;
            helper.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            helper.AddTriangle(start, start + 1, start + 2); helper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
