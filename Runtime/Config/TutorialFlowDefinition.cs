using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dreamy.Tutorial
{
    public sealed class TutorialFlowDefinition
    {
        [JsonProperty("id", Required = Required.Always)] public string Id { get; private set; }
        [JsonProperty("allowSkip")] public bool AllowSkip { get; private set; }
        [JsonProperty("steps", Required = Required.Always)] private List<TutorialStepDefinition> steps = new();
        [JsonProperty("stepMigrations")] private Dictionary<string, string> stepMigrations = new();
        [JsonIgnore] public IReadOnlyList<TutorialStepDefinition> Steps => steps?.AsReadOnly();
        [JsonIgnore] public IReadOnlyDictionary<string, string> StepMigrations => stepMigrations == null ? null : new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(stepMigrations);
    }
}
