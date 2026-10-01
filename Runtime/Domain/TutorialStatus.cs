namespace Dreamy.Tutorial
{
    public enum TutorialStatus { Idle, WaitingForTarget, ActiveStep, Suspended, Completed, Skipped }
    public enum TutorialCompletionMode { Next, TargetClick, HostSignal }
    public enum TutorialResult
    {
        Started, Advanced, Completed, Skipped, Suspended, Resumed, Updated,
        AlreadyActive, AlreadyCompleted, AlreadySkipped, Busy, InvalidFlow,
        InvalidToken, InvalidAction, NotActive, SkipNotAllowed, SaveFailed, MigrationRequired
    }
}
