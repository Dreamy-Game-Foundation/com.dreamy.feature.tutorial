using System;
using System.Diagnostics;
namespace Dreamy.Tutorial
{
    public sealed class TutorialWaitingOptions
    {
        // Zero disables the deadline. The clock must be monotonic and use seconds.
        public double TimeoutSeconds { get; set; }
        public Func<double> Clock { get; set; } = () => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        public Action<TutorialState> TargetTimedOut { get; set; }
        public Action<TutorialState> RecoverRequested { get; set; }
    }
}
