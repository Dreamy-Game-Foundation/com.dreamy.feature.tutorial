using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dreamy.Tutorial
{
    public sealed class TutorialStepDefinition
    {
        [JsonProperty("id", Required = Required.Always)] public string Id { get; private set; }
        [JsonProperty("messageKey", Required = Required.Always)] public string MessageKey { get; private set; }
        [JsonProperty("targetId")] public string TargetId { get; private set; }
        [JsonProperty("completionMode", Required = Required.Always), JsonConverter(typeof(StringEnumConverter))]
        public TutorialCompletionMode CompletionMode { get; private set; }
        [JsonProperty("signalKey")] public string SignalKey { get; private set; }
        [JsonProperty("blockOutsideTarget")] public bool BlockOutsideTarget { get; private set; }
    }
}
