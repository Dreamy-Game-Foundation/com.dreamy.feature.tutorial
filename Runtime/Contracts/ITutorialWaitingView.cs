using System;
namespace Dreamy.Tutorial
{
    /// <summary>Recovery intent is independent from save retry and never advances a step automatically.</summary>
    public interface ITutorialWaitingView
    {
        event Action RecoveryRequested;
        void RenderWaiting(TutorialState state, bool timedOut);
    }
}
