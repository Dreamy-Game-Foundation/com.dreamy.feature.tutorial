using System;
namespace Dreamy.Tutorial
{
    public sealed class TutorialState
    {
        public string FlowId { get; }
        public TutorialStepDefinition Step { get; }
        public TutorialStatus Status { get; }
        public Guid StepToken { get; }
        public bool AllowSkip { get; }
        public string Error { get; }
        public TutorialState(string flowId, TutorialStepDefinition step, TutorialStatus status,
            Guid token, bool allowSkip, string error = null)
        {
            FlowId = flowId; Step = step; Status = status; StepToken = token; AllowSkip = allowSkip; Error = error;
        }
    }
}
