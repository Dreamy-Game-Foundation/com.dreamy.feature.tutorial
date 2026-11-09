using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dreamy.Tutorial
{
    public sealed class TutorialStepDefinition
    {
        [JsonProperty("id", Required = Required.Always)] public string Id { get; private set; }
        [JsonProperty("messageKey", Required = Required.Always)] public string MessageKey { get; private set; }
        [JsonProperty("targetId")] public string TargetId { get; private set; }
        [JsonProperty("focusTargetIds")] private List<string> focusTargetIds;
        [JsonProperty("inputTargetIds")] private List<string> inputTargetIds;
        private IReadOnlyList<string> resolvedFocus, resolvedInput, legacyTargets;
        // Missing lists preserve the legacy single-target behavior; explicit [] means none.
        [JsonIgnore] public IReadOnlyList<string> FocusTargetIds => resolvedFocus ??= focusTargetIds?.AsReadOnly() ?? LegacyTargets;
        [JsonIgnore] public IReadOnlyList<string> InputTargetIds => resolvedInput ??= inputTargetIds?.AsReadOnly() ?? LegacyTargets;
        [JsonIgnore] private IReadOnlyList<string> LegacyTargets => legacyTargets ??= string.IsNullOrWhiteSpace(TargetId)
            ? System.Array.Empty<string>() : new[] { TargetId };
        [JsonIgnore] public bool RequiresTargets => !string.IsNullOrWhiteSpace(TargetId) || FocusTargetIds.Count > 0 || InputTargetIds.Count > 0;
        [JsonProperty("completionMode", Required = Required.Always), JsonConverter(typeof(StringEnumConverter))]
        public TutorialCompletionMode CompletionMode { get; private set; }
        [JsonProperty("signalKey")] public string SignalKey { get; private set; }
        [JsonProperty("blockOutsideTarget")] public bool BlockOutsideTarget { get; private set; }
    }
}
