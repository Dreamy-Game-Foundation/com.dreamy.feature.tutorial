using System;
using NUnit.Framework;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialPresenterTests
    {
        private sealed class Target : ITutorialTarget
        {
            public string Id => "button";
            public bool IsAvailable { get; set; } = true;
            public event Action Clicked;
            public void Click() => Clicked?.Invoke();
            public Action Capture() => Clicked;
        }
        private sealed class Resolver : ITutorialTargetResolver
        {
            public Target Target;
            public bool TryResolve(string id, out ITutorialTarget target) { target = Target; return target != null; }
        }
        private sealed class View : ITutorialView
        {
            public event Action NextRequested; public event Action SkipRequested; public event Action RetryRequested;
            public int Renders; public bool Hidden; public string Error;
            public void Render(TutorialState state, ITutorialTarget target) { Renders++; Hidden = false; }
            public void ShowError(string message) => Error = message;
            public void Hide() => Hidden = true;
            public void Next() => NextRequested?.Invoke();
            public void Skip() => SkipRequested?.Invoke();
            public void Retry() => RetryRequested?.Invoke();
        }
        [Test] public void ShowTwice_BindsOnceAndDisposeStopsInputs()
        {
            var model = new TutorialModel(TutorialModelTests.Catalog(), new TutorialTestStore());
            var view = new View(); using var presenter = new TutorialPresenter(model, view, new Resolver());
            model.TryStart("ui"); presenter.Show(); presenter.Show(); view.Next();
            Assert.That(model.GetState().Step.Id, Is.EqualTo("click"));
            presenter.Dispose(); view.Skip(); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.WaitingForTarget));
            Assert.That(view.Hidden, Is.True);
        }
        [Test] public void TargetAppearsAndDisappears_WithoutAdvancing()
        {
            var model = new TutorialModel(TutorialModelTests.Catalog(), new TutorialTestStore());
            var resolver = new Resolver(); using var presenter = new TutorialPresenter(model, new View(), resolver);
            model.TryStart("ui"); model.TryAdvance(model.GetState().StepToken); presenter.Show();
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.WaitingForTarget));
            resolver.Target = new Target(); presenter.Tick(); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            resolver.Target.IsAvailable = false; presenter.Tick(); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.WaitingForTarget));
            resolver.Target = new Target(); presenter.Tick(); resolver.Target.Click(); Assert.That(model.GetState().Step.Id, Is.EqualTo("signal"));
        }
        [Test] public void CapturedOldClick_DoesNotAdvanceNextStepOrTouchDisposedView()
        {
            var model = new TutorialModel(TutorialModelTests.Catalog(), new TutorialTestStore());
            var view = new View(); var resolver = new Resolver { Target = new Target() };
            var presenter = new TutorialPresenter(model, view, resolver);
            model.TryStart("ui"); model.TryAdvance(model.GetState().StepToken); presenter.Show();
            var old = resolver.Target.Capture(); resolver.Target.Click(); old();
            Assert.That(model.GetState().Step.Id, Is.EqualTo("signal"));
            presenter.Dispose(); int count = view.Renders; old(); Assert.That(view.Renders, Is.EqualTo(count));
        }
        [Test] public void HostSignalSaveFailure_RetryPersistsWithoutRepeatingAction()
        {
            var store = new TutorialTestStore(); var model = new TutorialModel(TutorialModelTests.Catalog(), store);
            var view = new View(); using var presenter = new TutorialPresenter(model, view, new Resolver { Target = new Target() });
            model.TryStart("ui"); presenter.Show(); view.Next();
            model.ReportTargetClick("button", model.GetState().StepToken); presenter.Tick();
            var token = model.GetState().StepToken; store.Fail = true;
            Assert.That(presenter.ReportSignal("success", token), Is.EqualTo(TutorialResult.SaveFailed));
            Assert.That(view.Error, Is.Not.Empty); store.Fail = false; view.Retry();
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.Completed));
        }
    }
}
