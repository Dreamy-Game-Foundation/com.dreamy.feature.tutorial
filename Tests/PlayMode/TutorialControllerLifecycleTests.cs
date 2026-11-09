using System;
using System.Collections.Generic;
using Dreamy.DataConfig;
using Dreamy.Tutorial.Integration;
using Dreamy.UI;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialControllerLifecycleTests
    {
        private sealed class Target : ITutorialTarget
        {
            public string Id { get; set; }
            public bool IsAvailable { get; set; } = true;
            public event Action Clicked;
            public void Click() => Clicked?.Invoke();
        }
        private sealed class Resolver : ITutorialTargetResolver
        {
            public readonly Dictionary<string, ITutorialTarget> Items = new();
            public bool TryResolve(string id, out ITutorialTarget value) => Items.TryGetValue(id, out value);
        }
        private sealed class View : ITutorialFocusView, ITutorialWaitingView
        {
            public event Action NextRequested; public event Action SkipRequested; public event Action RetryRequested;
            public event Action RecoveryRequested;
            public int FocusCount, InputCount, Renders;
            public bool TimedOut;
            public void Render(TutorialState state, ITutorialTarget target) { Renders++; }
            public void RenderTargets(TutorialState state, ITutorialTarget target, IReadOnlyList<ITutorialTarget> focus, IReadOnlyList<ITutorialTarget> input)
            { FocusCount = focus.Count; InputCount = input.Count; Render(state, target); }
            public void RenderWaiting(TutorialState state, bool timedOut) { TimedOut = timedOut; }
            public void ShowError(string message) { }
            public void Hide() { }
            public void Recover() => RecoveryRequested?.Invoke();
        }
        private static TutorialCatalogConfig Catalog(string extra = "", string mode = "Next")
        {
            var value = JsonConvert.DeserializeObject<TutorialCatalogConfig>("{\"catalogId\":\"extensions\",\"revision\":1,\"flows\":[{\"id\":\"flow\",\"allowSkip\":true,\"steps\":[{\"id\":\"one\",\"messageKey\":\"one\",\"completionMode\":\"" + mode + "\"" + extra + "}]}]}");
            value.Initialize("test"); return value;
        }
        [Test] public void Controller_AcceptsInterfaceView_ReopensAndRejectsSignalsWhileDisabled()
        {
            var root = new GameObject("Controller"); root.SetActive(false);
            try
            {
                var controller = root.AddComponent<TutorialController>();
                var model = new TutorialModel(Catalog(",\"signalKey\":\"done\"", "HostSignal"), new TutorialLifecycleTestStore());
                ITutorialView view = new View(); controller.Initialize(model, view, new Resolver()); model.TryStart("flow");
                root.SetActive(true); var token = model.GetState().StepToken;
                root.SetActive(false); Assert.That(controller.ReportSignal("done", token), Is.EqualTo(TutorialResult.NotActive));
                root.SetActive(true); Assert.That(controller.ReportSignal("done", token), Is.EqualTo(TutorialResult.InvalidToken));
                Assert.That(controller.ReportSignal("done", model.GetState().StepToken), Is.EqualTo(TutorialResult.Completed));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test] public void Controller_FactoryRegistersConcreteCustomView()
        {
            var root = new GameObject("Controller"); root.SetActive(false);
            try
            {
                var controller = root.AddComponent<TutorialController>(); var view = new View(); var resolver = new Resolver();
                var model = new TutorialModel(Catalog(), new TutorialLifecycleTestStore()); var factory = new PanelPresenterFactory();
                factory.Register<View>(v => new TutorialPresenter(model, v, resolver));
                controller.Initialize(factory, model, view, resolver); model.TryStart("flow"); root.SetActive(true);
                Assert.That(view.Renders, Is.GreaterThan(0));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
