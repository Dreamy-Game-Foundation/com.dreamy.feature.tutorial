using System;
namespace Dreamy.Tutorial
{
    public interface ITutorialView
    {
        event Action NextRequested;
        event Action SkipRequested;
        event Action RetryRequested;
        void Render(TutorialState state, ITutorialTarget target);
        void ShowError(string message);
        void Hide();
    }
}
