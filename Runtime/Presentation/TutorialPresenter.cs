using System;
using System.Collections.Generic;
using Dreamy.UI;
namespace Dreamy.Tutorial
{
    public sealed class TutorialPresenter : ITickedPanelPresenter
    {
        private readonly ITutorialService service;
        private readonly ITutorialView view;
        private readonly ITutorialTargetResolver resolver;
        private readonly TutorialWaitingOptions waiting;
        private readonly List<ITutorialTarget> focusTargets = new();
        private readonly List<ITutorialTarget> inputTargets = new();
        private Guid waitingToken;
        private double waitingSince;
        private bool timedOut;
        private ITutorialTarget target;
        private Action clickHandler;
        private Guid boundToken;
        private bool bound;
        private Func<TutorialResult> retry;
        public TutorialPresenter(ITutorialService service, ITutorialView view, ITutorialTargetResolver resolver, TutorialWaitingOptions waiting = null)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.waiting = waiting ?? new TutorialWaitingOptions();
            if (this.waiting.Clock == null || double.IsNaN(this.waiting.TimeoutSeconds) ||
                double.IsInfinity(this.waiting.TimeoutSeconds) || this.waiting.TimeoutSeconds < 0)
                throw new ArgumentException("Waiting requires a clock and a finite nonnegative timeout.", nameof(waiting));
        }
        public void Show()
        {
            if (!bound)
            {
                bound = true; service.StateChanged += Render; view.NextRequested += Next; view.SkipRequested += Skip; view.RetryRequested += Retry;
                if (view is ITutorialWaitingView recovery) recovery.RecoveryRequested += Recover;
            }
            Tick();
        }
        // Host calls once per frame after layout; target changes do not mutate the gameplay service from render callbacks.
        public void Tick()
        {
            if (!bound) return;
            var state = service.GetState();
            ITutorialTarget resolved = null;
            if (state.Step != null && state.Status != TutorialStatus.Suspended && !string.IsNullOrWhiteSpace(state.Step.TargetId))
                resolver.TryResolve(state.Step.TargetId, out resolved);
            if (!ReferenceEquals(target, resolved) || boundToken != state.StepToken)
            {
                UnbindTarget(); target = resolved; boundToken = state.StepToken;
                if (target != null)
                {
                    string id = target.Id; Guid captured = boundToken;
                    clickHandler = () => Execute(() => service.ReportTargetClick(id, captured));
                    target.Clicked += clickHandler;
                }
            }
            focusTargets.Clear(); inputTargets.Clear();
            if (state.Step != null && state.Status != TutorialStatus.Suspended)
            {
                bool available = Resolve(state.Step.FocusTargetIds, focusTargets);
                available &= Resolve(state.Step.InputTargetIds, inputTargets);
                if (!string.IsNullOrWhiteSpace(state.Step.TargetId)) available &= target?.IsAvailable ?? false;
                service.SetTargetAvailable(state.StepToken, available);
            }
            UpdateWaiting();
            Render();
        }
        private bool Resolve(IReadOnlyList<string> ids, List<ITutorialTarget> results)
        {
            bool available = true;
            foreach (var id in ids)
            {
                resolver.TryResolve(id, out var item);
                available &= item?.IsAvailable ?? false;
                if (item != null) results.Add(item);
            }
            return available;
        }
        private void UpdateWaiting()
        {
            var state = service.GetState();
            if (state.Status != TutorialStatus.WaitingForTarget)
            { waitingToken = Guid.Empty; timedOut = false; return; }
            double now = waiting.Clock();
            if (waitingToken != state.StepToken)
            { waitingToken = state.StepToken; waitingSince = now; timedOut = false; }
            if (!timedOut && waiting.TimeoutSeconds > 0 && now - waitingSince >= waiting.TimeoutSeconds)
            {
                timedOut = true;
                waiting.TargetTimedOut?.Invoke(state);
            }
        }
        private void Recover()
        {
            var state = service.GetState();
            if (bound && state.Status == TutorialStatus.WaitingForTarget)
                waiting.RecoverRequested?.Invoke(state);
        }
        private void Render()
        {
            if (!bound) return;
            var state = service.GetState();
            if (state.Status == TutorialStatus.Idle || state.Status == TutorialStatus.Completed || state.Status == TutorialStatus.Skipped || state.Status == TutorialStatus.Suspended)
                view.Hide();
            else
            {
                var completionTarget = boundToken == state.StepToken ? target : null;
                // A state notification can precede Tick after advancing. Never render stale focus/input lists.
                if (view is ITutorialFocusView multi)
                    multi.RenderTargets(state, completionTarget,
                        boundToken == state.StepToken ? focusTargets : Array.Empty<ITutorialTarget>(),
                        boundToken == state.StepToken ? inputTargets : Array.Empty<ITutorialTarget>());
                else view.Render(state, completionTarget);
                if (state.Status == TutorialStatus.WaitingForTarget && view is ITutorialWaitingView recovery)
                    recovery.RenderWaiting(state, waitingToken == state.StepToken && timedOut);
            }
        }
        private void Next()
        {
            Guid captured = boundToken;
            Execute(() => service.TryAdvance(captured));
        }
        private void Skip() => Execute(() => service.TrySkip());
        private void Retry() { if (retry != null) Execute(retry); }
        public TutorialResult ReportSignal(string key, Guid token) => Execute(() => service.ReportSignal(key, token));
        private TutorialResult Execute(Func<TutorialResult> command)
        {
            if (!bound) return TutorialResult.NotActive;
            TutorialResult result = command();
            retry = result == TutorialResult.SaveFailed ? command : null;
            Handle(result); return result;
        }
        private void Handle(TutorialResult result)
        {
            if (result == TutorialResult.SaveFailed) view.ShowError(service.GetState().Error ?? "Tutorial save failed. Retry without repeating the gameplay action.");
            Tick();
        }
        public void Dispose()
        {
            if (!bound) return;
            bound = false; service.StateChanged -= Render; view.NextRequested -= Next; view.SkipRequested -= Skip; view.RetryRequested -= Retry; retry = null;
            if (view is ITutorialWaitingView recovery) recovery.RecoveryRequested -= Recover;
            waitingToken = Guid.Empty; timedOut = false; focusTargets.Clear(); inputTargets.Clear();
            UnbindTarget(); view.Hide();
        }
        private void UnbindTarget()
        {
            if (target != null && clickHandler != null) target.Clicked -= clickHandler;
            target = null; clickHandler = null; boundToken = Guid.Empty;
        }
    }
}
