using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Tutorial.Integration
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialSpotlight : MaskableGraphic, ICanvasRaycastFilter
    {
        private readonly List<Rect> screenHoles = new();
        private readonly List<Rect> localHoles = new();
        private readonly List<ITutorialScreenTarget> inputTargets = new();
        private readonly List<float> xs = new(), ys = new();
        private bool blocking;
        public void SetTarget(ITutorialScreenTarget value, bool block)
        {
            blocking = block; screenHoles.Clear(); inputTargets.Clear();
            if (value != null)
            {
                if (value.IsAvailable && value.TryGetScreenRect(out var rect)) screenHoles.Add(rect);
                inputTargets.Add(value);
            }
            SetVerticesDirty();
        }
        public void SetTargets(IReadOnlyList<ITutorialTarget> focus, IReadOnlyList<ITutorialTarget> input, bool block)
        {
            blocking = block; screenHoles.Clear(); inputTargets.Clear();
            if (focus != null)
                foreach (var item in focus)
                    if (item is ITutorialScreenTarget target && target.IsAvailable && target.TryGetScreenRect(out var rect))
                        screenHoles.Add(rect);
            if (input != null)
                foreach (var item in input)
                    if (item is ITutorialScreenTarget target) inputTargets.Add(target);
            SetVerticesDirty();
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!blocking) return false;
            foreach (var target in inputTargets)
                if (target.IsAvailable && target.ContainsInput(screenPoint)) return false;
            return true;
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); Rect outer = rectTransform.rect;
            if (screenHoles.Count == 0) { Quad(helper, outer); return; }
            if (canvas == null) return;
            var camera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            localHoles.Clear(); xs.Clear(); ys.Clear();
            xs.Add(outer.xMin); xs.Add(outer.xMax); ys.Add(outer.yMin); ys.Add(outer.yMax);
            foreach (var hole in screenHoles)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, hole.min, camera, out var min);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, hole.max, camera, out var max);
                min = Vector2.Max(outer.min, Vector2.Min(outer.max, min));
                max = Vector2.Max(min, Vector2.Min(outer.max, max));
                if (max.x <= min.x || max.y <= min.y) continue;
                localHoles.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
                xs.Add(min.x); xs.Add(max.x); ys.Add(min.y); ys.Add(max.y);
            }
            xs.Sort(); ys.Sort();
            for (int x = 1; x < xs.Count; x++)
                for (int y = 1; y < ys.Count; y++)
                {
                    var cell = Rect.MinMaxRect(xs[x-1], ys[y-1], xs[x], ys[y]);
                    bool inside = false;
                    foreach (var hole in localHoles) if (hole.Contains(cell.center)) { inside = true; break; }
                    if (!inside) Quad(helper, cell);
                }
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
