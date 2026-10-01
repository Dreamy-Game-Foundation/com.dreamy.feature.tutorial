using System;
using System.Collections.Generic;
using System.Linq;
using Dreamy.DataConfig;
using Newtonsoft.Json;

namespace Dreamy.Tutorial
{
    public sealed class TutorialCatalogConfig : ConfigBase
    {
        [JsonProperty("catalogId", Required = Required.Always)] public string CatalogId { get; private set; }
        [JsonProperty("revision", Required = Required.Always)] public int Revision { get; private set; }
        [JsonProperty("flows", Required = Required.Always)] private List<TutorialFlowDefinition> flows = new();
        [JsonIgnore] public IReadOnlyList<TutorialFlowDefinition> Flows => flows.AsReadOnly();

        public override void Initialize(string documentName)
        {
            void Fail(string message) => throw new DataConfigException(documentName, message);
            if (string.IsNullOrWhiteSpace(CatalogId) || Revision < 1 || flows == null || flows.Count == 0)
                Fail("Tutorial requires a catalogId, positive revision and nonempty flows.");
            var flowIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var flow in flows)
            {
                if (flow == null || string.IsNullOrWhiteSpace(flow.Id) || !flowIds.Add(flow.Id))
                    Fail("Flow IDs must be nonempty and unique.");
                if (flow.Steps == null || flow.Steps.Count == 0) Fail("Flow steps must be nonempty.");
                var stepIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var step in flow.Steps)
                {
                    if (step == null || string.IsNullOrWhiteSpace(step.Id) || !stepIds.Add(step.Id) ||
                        string.IsNullOrWhiteSpace(step.MessageKey) || !Enum.IsDefined(typeof(TutorialCompletionMode), step.CompletionMode))
                        Fail("Step IDs, message keys and completion modes must be valid.");
                    bool hasTarget = !string.IsNullOrWhiteSpace(step.TargetId);
                    if ((step.CompletionMode == TutorialCompletionMode.TargetClick || step.BlockOutsideTarget) && !hasTarget)
                        Fail("Target click and target blocking require a targetId.");
                    if (step.CompletionMode == TutorialCompletionMode.HostSignal && string.IsNullOrWhiteSpace(step.SignalKey))
                        Fail("HostSignal requires a signalKey.");
                }
                if (flow.StepMigrations == null || flow.StepMigrations.Any(m => string.IsNullOrWhiteSpace(m.Key) || !stepIds.Contains(m.Value)))
                    Fail("Each migration must map an old step ID to an existing step ID.");
            }
        }
    }
}
