using System;
namespace Dreamy.Tutorial
{
    public sealed class TutorialPresenter : IDisposable
    {
        private readonly ITutorialService service;
        private readonly ITutorialView view;
        private readonly ITutorialTargetResolver resolver;
        private ITutorialTarget target;
        private Action clickHandler;
        private Guid boundToken;
        private bool bound;
        private Func<TutorialResult> retry;
        public TutorialPresenter(ITutorialService service, ITutorialView view, ITutorialTargetResolver resolver)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }
        public void Show()
        {
            if (!bound)
            {
                bound = true; service.StateChanged += Render; view.NextRequested += Next; view.SkipRequested += Skip; view.RetryRequested += Retry;
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
            if (state.Step != null && state.Status != TutorialStatus.Suspended)
                service.SetTargetAvailable(state.StepToken, target?.IsAvailable ?? false);
            Render();
        }
        private void Render()
        {
            if (!bound) return;
            var state = service.GetState();
            if (state.Status == TutorialStatus.Idle || state.Status == TutorialStatus.Completed || state.Status == TutorialStatus.Skipped || state.Status == TutorialStatus.Suspended)
                view.Hide();
            else view.Render(state, boundToken == state.StepToken ? target : null);
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
            UnbindTarget(); view.Hide();
        }
        private void UnbindTarget()
        {
            if (target != null && clickHandler != null) target.Clicked -= clickHandler;
            target = null; clickHandler = null; boundToken = Guid.Empty;
        }
    }
}
