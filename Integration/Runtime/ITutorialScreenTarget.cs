using UnityEngine;
namespace Dreamy.Tutorial.Integration
{
    public interface ITutorialScreenTarget : ITutorialTarget
    {
        bool TryGetScreenRect(out Rect rect);
        bool ContainsInput(Vector2 screenPoint);
    }
}
