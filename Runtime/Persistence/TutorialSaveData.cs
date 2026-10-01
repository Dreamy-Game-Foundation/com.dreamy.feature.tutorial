using System.Collections.Generic;
using Dreamy.Datasave;
using Newtonsoft.Json;
namespace Dreamy.Tutorial
{
    public sealed class TutorialSaveData : SaveData
    {
        [JsonProperty("catalogId")] public string CatalogId { get; set; }
        [JsonProperty("revision")] public int Revision { get; set; }
        [JsonProperty("flows")] public Dictionary<string, TutorialFlowProgress> Flows { get; set; } = new();
        public override int Version => 1;
    }
}
