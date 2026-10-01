using System;
using System.Collections.Generic;
using System.Linq;
using Dreamy.Datasave;
using Newtonsoft.Json;

namespace Dreamy.Tutorial
{
    public sealed class TutorialModel : ITutorialService
    {
        private readonly TutorialCatalogConfig catalog;
        private readonly IDatasaveService datasave;
        private readonly string saveKey;
        private TutorialSaveData save;
        private TutorialFlowDefinition flow;
        private TutorialStepDefinition step;
        private TutorialStatus status;
        private Guid token;
        private bool busy;
        private bool wasActive;
        private string error;
        public event Action StateChanged;

        public TutorialModel(TutorialCatalogConfig catalog, IDatasaveService datasave, string saveKey = "tutorials")
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            this.catalog = JsonConvert.DeserializeObject<TutorialCatalogConfig>(JsonConvert.SerializeObject(catalog));
            this.catalog.Initialize("tutorialCatalog");
            this.datasave = datasave ?? throw new ArgumentNullException(nameof(datasave));
            this.saveKey = string.IsNullOrWhiteSpace(saveKey) ? throw new ArgumentException("Save key is required.") : saveKey;
            save = Clone(datasave.Load<TutorialSaveData>(saveKey));
            save.Flows ??= new Dictionary<string, TutorialFlowProgress>();
        }

        public TutorialState GetState() => new(flow?.Id, step, status, token, flow?.AllowSkip ?? false, error);

        public TutorialResult TryStart(string flowId)
        {
            if (busy) return TutorialResult.Busy;
            if (status == TutorialStatus.ActiveStep || status == TutorialStatus.WaitingForTarget || status == TutorialStatus.Suspended)
                return flow?.Id == flowId ? TutorialResult.AlreadyActive : TutorialResult.Busy;
            var nextFlow = catalog.Flows.FirstOrDefault(f => f.Id == flowId);
            if (nextFlow == null) return TutorialResult.InvalidFlow;
            if (!string.IsNullOrEmpty(save.CatalogId) && save.CatalogId != catalog.CatalogId)
                return TutorialResult.MigrationRequired;
            save.Flows.TryGetValue(flowId, out var progress);
            if (progress?.Status == TutorialStatus.Completed) return TutorialResult.AlreadyCompleted;
            if (progress?.Status == TutorialStatus.Skipped) return TutorialResult.AlreadySkipped;
            string id = progress?.NextStepId ?? nextFlow.Steps[0].Id;
            var nextStep = nextFlow.Steps.FirstOrDefault(s => s.Id == id);
            if (nextStep == null && nextFlow.StepMigrations.TryGetValue(id, out string migrated))
                nextStep = nextFlow.Steps.First(s => s.Id == migrated);
            if (nextStep == null)
            {
                flow = nextFlow; step = null; token = Guid.Empty; status = TutorialStatus.Suspended;
                error = $"Checkpoint step '{id}' requires an explicit migration.";
                Publish(); return TutorialResult.MigrationRequired;
            }
            if (!Persist(nextFlow, nextStep, TutorialStatus.Suspended)) return TutorialResult.SaveFailed;
            flow = nextFlow; Activate(nextStep); Publish(); return TutorialResult.Started;
        }

        public TutorialResult TryAdvance(Guid stepToken) => CompleteStep(stepToken, TutorialCompletionMode.Next, null);
        public TutorialResult ReportTargetClick(string targetId, Guid stepToken) => CompleteStep(stepToken, TutorialCompletionMode.TargetClick, targetId);
        public TutorialResult ReportSignal(string signalKey, Guid stepToken) => CompleteStep(stepToken, TutorialCompletionMode.HostSignal, signalKey);

        private TutorialResult CompleteStep(Guid stepToken, TutorialCompletionMode mode, string key)
        {
            if (busy) return TutorialResult.Busy;
            if (stepToken == Guid.Empty || stepToken != token) return TutorialResult.InvalidToken;
            if (step == null || (status != TutorialStatus.ActiveStep &&
                !(mode == TutorialCompletionMode.HostSignal && status == TutorialStatus.WaitingForTarget && wasActive)))
                return TutorialResult.NotActive;
            if (step.CompletionMode != mode || (mode == TutorialCompletionMode.TargetClick && step.TargetId != key) ||
                (mode == TutorialCompletionMode.HostSignal && step.SignalKey != key)) return TutorialResult.InvalidAction;
            int index = flow.Steps.ToList().FindIndex(s => s.Id == step.Id);
            var next = index + 1 < flow.Steps.Count ? flow.Steps[index + 1] : null;
            if (!Persist(flow, next, next == null ? TutorialStatus.Completed : TutorialStatus.Suspended))
                return TutorialResult.SaveFailed;
            if (next == null) { step = null; token = Guid.Empty; status = TutorialStatus.Completed; }
            else Activate(next);
            Publish(); return next == null ? TutorialResult.Completed : TutorialResult.Advanced;
        }

        public TutorialResult SetTargetAvailable(Guid stepToken, bool available)
        {
            if (busy) return TutorialResult.Busy;
            if (stepToken == Guid.Empty || stepToken != token) return TutorialResult.InvalidToken;
            if (step == null || status == TutorialStatus.Suspended) return TutorialResult.NotActive;
            var nextStatus = string.IsNullOrWhiteSpace(step.TargetId) || available ? TutorialStatus.ActiveStep : TutorialStatus.WaitingForTarget;
            if (nextStatus == TutorialStatus.ActiveStep) wasActive = true;
            if (status != nextStatus) { status = nextStatus; Publish(); }
            return TutorialResult.Updated;
        }

        public TutorialResult Suspend()
        {
            if (busy) return TutorialResult.Busy;
            if (status != TutorialStatus.ActiveStep && status != TutorialStatus.WaitingForTarget) return TutorialResult.NotActive;
            status = TutorialStatus.Suspended; token = Guid.Empty; Publish(); return TutorialResult.Suspended;
        }

        public TutorialResult Resume()
        {
            if (busy) return TutorialResult.Busy;
            if (status != TutorialStatus.Suspended) return TutorialResult.NotActive;
            if (step == null) return TutorialResult.MigrationRequired;
            Activate(step); Publish(); return TutorialResult.Resumed;
        }

        public TutorialResult TrySkip()
        {
            if (busy) return TutorialResult.Busy;
            if (flow == null || (status != TutorialStatus.ActiveStep && status != TutorialStatus.WaitingForTarget && status != TutorialStatus.Suspended))
                return TutorialResult.NotActive;
            if (!flow.AllowSkip) return TutorialResult.SkipNotAllowed;
            if (!Persist(flow, null, TutorialStatus.Skipped)) return TutorialResult.SaveFailed;
            step = null; token = Guid.Empty; status = TutorialStatus.Skipped; Publish(); return TutorialResult.Skipped;
        }

        private void Activate(TutorialStepDefinition definition)
        {
            step = definition; token = Guid.NewGuid(); error = null;
            wasActive = string.IsNullOrWhiteSpace(step.TargetId);
            status = wasActive ? TutorialStatus.ActiveStep : TutorialStatus.WaitingForTarget;
        }

        private bool Persist(TutorialFlowDefinition definition, TutorialStepDefinition nextStep, TutorialStatus nextStatus)
        {
            busy = true;
            try
            {
                var candidate = Clone(save);
                candidate.CatalogId = catalog.CatalogId; candidate.Revision = catalog.Revision;
                candidate.Flows[definition.Id] = new TutorialFlowProgress { Status = nextStatus, NextStepId = nextStep?.Id };
                datasave.Save(Clone(candidate), saveKey);
                save = candidate; error = null; return true;
            }
            catch (Exception exception) { error = exception.Message; return false; }
            finally { busy = false; }
        }
        // Reject reentrant commands from view/state subscribers as well as persistence callbacks.
        private void Publish()
        {
            busy = true;
            try { StateChanged?.Invoke(); }
            finally { busy = false; }
        }
        private static TutorialSaveData Clone(TutorialSaveData data) => data == null ? new() :
            JsonConvert.DeserializeObject<TutorialSaveData>(JsonConvert.SerializeObject(data));
    }
}
