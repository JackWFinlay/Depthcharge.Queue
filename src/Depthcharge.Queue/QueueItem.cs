using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace Depthcharge.Queue
{
    public class QueueItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public string Protocol { get; set; }

        [JsonProperty(PropertyName ="url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }

        [JsonProperty(PropertyName = "indexed")]
        public bool Indexed { get; set; }

        [JsonProperty(PropertyName = "requested")]
        public bool Requested { get; set; }
    }
}