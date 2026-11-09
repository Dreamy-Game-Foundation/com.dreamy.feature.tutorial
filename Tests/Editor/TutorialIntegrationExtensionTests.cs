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
    public sealed class TutorialIntegrationExtensionTests
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
        [Test] public void MultiFocus_WaitsForAllTargets_AndDoesNotChangeClickAuthority()
        {
            var model = new TutorialModel(Catalog(",\"targetId\":\"click\",\"focusTargetIds\":[\"click\",\"pickup\"],\"inputTargetIds\":[\"click\"]", "TargetClick"), new TutorialTestStore());
            var resolver = new Resolver(); var click = new Target { Id = "click" }; var pickup = new Target { Id = "pickup" };
            resolver.Items.Add(click.Id, click); var view = new View();
            using var presenter = new TutorialPresenter(model, view, resolver); model.TryStart("flow"); presenter.Show();
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.WaitingForTarget));
            resolver.Items.Add(pickup.Id, pickup); presenter.Tick();
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            Assert.That(view.FocusCount, Is.EqualTo(2)); Assert.That(view.InputCount, Is.EqualTo(1));
            pickup.Click(); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.ActiveStep));
            click.Click(); Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.Completed));
        }
        [Test] public void WaitingTimeout_NotifiesOnce_ResetsOnRecovery_AndNeverAdvances()
        {
            double clock = 0; int notifications = 0, recoveries = 0;
            var options = new TutorialWaitingOptions { TimeoutSeconds = 2, Clock = () => clock,
                TargetTimedOut = _ => notifications++, RecoverRequested = _ => recoveries++ };
            var model = new TutorialModel(Catalog(",\"focusTargetIds\":[\"pickup\"]"), new TutorialTestStore());
            var view = new View(); var resolver = new Resolver(); var presenter = new TutorialPresenter(model, view, resolver, options);
            model.TryStart("flow"); presenter.Show(); var token = model.GetState().StepToken;
            clock = 3; presenter.Tick(); presenter.Tick(); view.Recover();
            Assert.That(notifications, Is.EqualTo(1)); Assert.That(recoveries, Is.EqualTo(1)); Assert.That(view.TimedOut, Is.True);
            Assert.That(model.GetState().StepToken, Is.EqualTo(token));
            resolver.Items.Add("pickup", new Target { Id = "pickup" }); presenter.Tick();
            resolver.Items.Clear(); presenter.Tick(); clock = 6; presenter.Tick(); Assert.That(notifications, Is.EqualTo(2));
            model.Suspend(); presenter.Tick(); clock = 100; presenter.Tick(); Assert.That(notifications, Is.EqualTo(2));
            presenter.Dispose(); view.Recover(); Assert.That(recoveries, Is.EqualTo(1));
        }
        [TestCase(",\"focusTargetIds\":[\"a\",\"a\"]")]
        [TestCase(",\"inputTargetIds\":[\"\"]")]
        [TestCase(",\"inputTargetIds\":[],\"blockOutsideTarget\":true")]
        public void InvalidTargetLists_AreRejected(string extra) => Assert.Throws<DataConfigException>(() => Catalog(extra));
        [Test] public void LegacyTarget_AndExplicitEmptyFocus_HaveDistinctSemantics()
        {
            var legacy = Catalog(",\"targetId\":\"a\"").Flows[0].Steps[0];
            Assert.That(legacy.FocusTargetIds, Is.EqualTo(new[] { "a" }));
            Assert.That(legacy.InputTargetIds, Is.EqualTo(new[] { "a" }));
            var explicitEmpty = Catalog(",\"targetId\":\"a\",\"focusTargetIds\":[]").Flows[0].Steps[0];
            Assert.That(explicitEmpty.FocusTargetIds, Is.Empty); Assert.That(explicitEmpty.RequiresTargets, Is.True);
        }
    }
}
