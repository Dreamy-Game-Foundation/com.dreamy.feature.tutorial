using System.Collections.Generic;
namespace Dreamy.Tutorial
{
    /// <summary>Optional view capability. Lists are borrowed until the next Tick; do not retain them.</summary>
    public interface ITutorialFocusView : ITutorialView
    {
        void RenderTargets(TutorialState state, ITutorialTarget completionTarget,
            IReadOnlyList<ITutorialTarget> focusTargets, IReadOnlyList<ITutorialTarget> inputTargets);
    }
}
