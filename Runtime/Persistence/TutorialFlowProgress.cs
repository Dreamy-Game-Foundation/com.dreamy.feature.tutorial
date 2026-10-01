using Newtonsoft.Json;
namespace Dreamy.Tutorial
{
    public sealed class TutorialFlowProgress
    {
        [JsonProperty("status")] public TutorialStatus Status { get; set; }
        [JsonProperty("nextStepId")] public string NextStepId { get; set; }
    }
}
