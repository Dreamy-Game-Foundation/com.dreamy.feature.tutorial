using System;
using Dreamy.DataConfig;
using Newtonsoft.Json;
using NUnit.Framework;
namespace Dreamy.Tutorial.Tests
{
    public sealed class TutorialModelTests
    {
        internal const string Json = @"{""catalogId"":""test"",""revision"":1,""flows"":[{""id"":""ui"",""allowSkip"":true,""steps"":[{""id"":""intro"",""messageKey"":""hello"",""completionMode"":""Next""},{""id"":""click"",""messageKey"":""click"",""completionMode"":""TargetClick"",""targetId"":""button""},{""id"":""signal"",""messageKey"":""action"",""completionMode"":""HostSignal"",""signalKey"":""success""}]},{""id"":""other"",""steps"":[{""id"":""one"",""messageKey"":""one"",""completionMode"":""Next""}]}]}";
        internal static TutorialCatalogConfig Catalog(string json = Json) => JsonConvert.DeserializeObject<TutorialCatalogConfig>(json);
        private static TutorialModel Model(TutorialTestStore store = null) => new(Catalog(), store ?? new TutorialTestStore());
        [Test] public void Start_DuplicateAndBusyAreExplicit()
        {
            var model = Model(); Assert.That(model.TryStart("missing"), Is.EqualTo(TutorialResult.InvalidFlow));
            Assert.That(model.TryStart("ui"), Is.EqualTo(TutorialResult.Started));
            var token = model.GetState().StepToken;
            Assert.That(model.TryStart("ui"), Is.EqualTo(TutorialResult.AlreadyActive));
            Assert.That(model.TryStart("other"), Is.EqualTo(TutorialResult.Busy));
            Assert.That(model.GetState().StepToken, Is.EqualTo(token));
        }
        [Test] public void Advance_RejectsDoubleClickAndWrongCompletionMode()
        {
            var model = Model(); model.TryStart("ui"); var token = model.GetState().StepToken;
            Assert.That(model.TryAdvance(token), Is.EqualTo(TutorialResult.Advanced));
            Assert.That(model.TryAdvance(token), Is.EqualTo(TutorialResult.InvalidToken));
            model.SetTargetAvailable(model.GetState().StepToken, true);
            Assert.That(model.TryAdvance(model.GetState().StepToken), Is.EqualTo(TutorialResult.InvalidAction));
        }
        [Test] public void Click_RequiresAvailableAndMatchingTarget()
        {
            var model = Model(); model.TryStart("ui"); model.TryAdvance(model.GetState().StepToken);
            var token = model.GetState().StepToken;
            Assert.That(model.ReportTargetClick("button", token), Is.EqualTo(TutorialResult.NotActive));
            model.SetTargetAvailable(token, true);
            Assert.That(model.ReportTargetClick("wrong", token), Is.EqualTo(TutorialResult.InvalidAction));
            Assert.That(model.ReportTargetClick("button", token), Is.EqualTo(TutorialResult.Advanced));
            Assert.That(model.ReportSignal("wrong", model.GetState().StepToken), Is.EqualTo(TutorialResult.InvalidAction));
            Assert.That(model.ReportSignal("success", model.GetState().StepToken), Is.EqualTo(TutorialResult.Completed));
        }
        [Test] public void NavigationSignal_CompletesAfterTargetDisappears()
        {
            string json = Json.Replace("TargetClick", "HostSignal").Replace("\"targetId\":\"button\"", "\"targetId\":\"button\",\"signalKey\":\"opened\"");
            var model = new TutorialModel(Catalog(json), new TutorialTestStore());
            model.TryStart("ui"); model.TryAdvance(model.GetState().StepToken);
            var token = model.GetState().StepToken;
            Assert.That(model.ReportSignal("opened", token), Is.EqualTo(TutorialResult.NotActive));
            model.SetTargetAvailable(token, true); model.SetTargetAvailable(token, false);
            Assert.That(model.ReportSignal("opened", token), Is.EqualTo(TutorialResult.Advanced));
        }
        [Test] public void CompletedFlow_DoesNotReplayAfterReload()
        {
            var store = new TutorialTestStore(); var model = Model(store); model.TryStart("other"); model.TryAdvance(model.GetState().StepToken);
            Assert.That(Model(store).TryStart("other"), Is.EqualTo(TutorialResult.AlreadyCompleted));
        }
        [Test] public void Resume_UsesStepIdAndNewSessionToken()
        {
            var store = new TutorialTestStore(); var model = Model(store); model.TryStart("ui"); model.TryAdvance(model.GetState().StepToken);
            var old = model.GetState().StepToken; var restored = Model(store); restored.TryStart("ui");
            Assert.That(restored.GetState().Step.Id, Is.EqualTo("click"));
            Assert.That(restored.GetState().StepToken, Is.Not.EqualTo(old));
        }
        [Test] public void TargetLoss_WaitsWithoutAdvancing()
        {
            var model = Model(); model.TryStart("ui"); model.TryAdvance(model.GetState().StepToken);
            var token = model.GetState().StepToken; model.SetTargetAvailable(token, true); model.SetTargetAvailable(token, false);
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.WaitingForTarget));
            model.SetTargetAvailable(token, true); Assert.That(model.GetState().Step.Id, Is.EqualTo("click"));
        }
        [Test] public void Suspend_RevokesCallbackToken()
        {
            var model = Model(); model.TryStart("ui"); var old = model.GetState().StepToken;
            model.Suspend(); Assert.That(model.TryAdvance(old), Is.EqualTo(TutorialResult.InvalidToken));
            model.Resume(); Assert.That(model.GetState().StepToken, Is.Not.EqualTo(old));
        }
        [Test] public void Skip_IsTerminalAndDistinctFromComplete()
        {
            var store = new TutorialTestStore(); var model = Model(store); model.TryStart("ui");
            Assert.That(model.TrySkip(), Is.EqualTo(TutorialResult.Skipped));
            Assert.That(Model(store).TryStart("ui"), Is.EqualTo(TutorialResult.AlreadySkipped));
        }
        [Test] public void MandatoryFlow_CannotSkip()
        {
            var model = Model(); model.TryStart("other"); Assert.That(model.TrySkip(), Is.EqualTo(TutorialResult.SkipNotAllowed));
        }
        [Test] public void FailedStart_DoesNotCreateSession()
        {
            var store = new TutorialTestStore { Fail = true }; var model = Model(store);
            Assert.That(model.TryStart("ui"), Is.EqualTo(TutorialResult.SaveFailed));
            Assert.That(model.GetState().Status, Is.EqualTo(TutorialStatus.Idle));
            store.Fail = false; Assert.That(model.TryStart("ui"), Is.EqualTo(TutorialResult.Started));
        }
        [Test] public void FailedAdvance_PreservesTokenAndCheckpointForRetry()
        {
            var store = new TutorialTestStore(); var model = Model(store); model.TryStart("ui"); var token = model.GetState().StepToken;
            store.Fail = true; Assert.That(model.TryAdvance(token), Is.EqualTo(TutorialResult.SaveFailed));
            Assert.That(model.GetState().Step.Id, Is.EqualTo("intro")); Assert.That(model.GetState().StepToken, Is.EqualTo(token));
            Assert.That(store.Load<TutorialSaveData>("tutorials").Flows["ui"].NextStepId, Is.EqualTo("intro"));
            store.Fail = false; Assert.That(model.TryAdvance(token), Is.EqualTo(TutorialResult.Advanced));
        }
        [Test] public void PersistenceAndSubscriberReentry_ReturnBusy()
        {
            var store = new TutorialTestStore(); var model = Model(store);
            store.OnSave = () => Assert.That(model.TryStart("other"), Is.EqualTo(TutorialResult.Busy));
            model.StateChanged += () => Assert.That(model.TryAdvance(model.GetState().StepToken), Is.EqualTo(TutorialResult.Busy));
            model.TryStart("ui"); Assert.That(model.GetState().Step.Id, Is.EqualTo("intro"));
        }
        [Test] public void RemovedCheckpoint_RequiresMigration_AndExplicitMappingResumes()
        {
            var store = new TutorialTestStore(); var model = Model(store); model.TryStart("ui");
            string changed = Json.Replace("\"intro\"", "\"newIntro\"");
            var migrated = new TutorialModel(Catalog(changed), store);
            Assert.That(migrated.TryStart("ui"), Is.EqualTo(TutorialResult.MigrationRequired));
            Assert.That(migrated.GetState().Status, Is.EqualTo(TutorialStatus.Suspended));
            changed = changed.Replace("\"allowSkip\":true", "\"stepMigrations\":{\"intro\":\"newIntro\"},\"allowSkip\":true");
            migrated = new TutorialModel(Catalog(changed), store);
            Assert.That(migrated.TryStart("ui"), Is.EqualTo(TutorialResult.Started));
            Assert.That(migrated.GetState().Step.Id, Is.EqualTo("newIntro"));
        }
        [Test] public void CatalogIdentityChange_DoesNotSilentlyResetProgress()
        {
            var store = new TutorialTestStore(); Model(store).TryStart("ui");
            var model = new TutorialModel(Catalog(Json.Replace("\"test\"", "\"different\"")), store);
            Assert.That(model.TryStart("ui"), Is.EqualTo(TutorialResult.MigrationRequired));
        }
        [TestCase("\"revision\":1", "\"revision\":0")]
        [TestCase("\"targetId\":\"button\"", "\"targetId\":\"\"")]
        [TestCase("\"signalKey\":\"success\"", "\"signalKey\":\"\"")]
        [TestCase("\"id\":\"click\"", "\"id\":\"intro\"")]
        [TestCase("\"completionMode\":\"Next\"", "\"completionMode\":99")]
        public void InvalidConfig_IsRejected(string oldValue, string newValue) =>
            Assert.Throws<DataConfigException>(() => Catalog(Json.Replace(oldValue, newValue)).Initialize("test"));
    }
}
