using System;
namespace Dreamy.Tutorial
{
    public interface ITutorialService
    {
        event Action StateChanged;
        TutorialState GetState();
        TutorialResult TryStart(string flowId);
        TutorialResult TryAdvance(Guid stepToken);
        TutorialResult ReportTargetClick(string targetId, Guid stepToken);
        TutorialResult ReportSignal(string signalKey, Guid stepToken);
        TutorialResult SetTargetAvailable(Guid stepToken, bool available);
        TutorialResult Suspend();
        TutorialResult Resume();
        TutorialResult TrySkip();
    }
}
