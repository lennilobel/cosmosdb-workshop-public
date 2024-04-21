using Newtonsoft.Json;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
    public class BulkDeleteResponse
    {
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "continuationFlag")]
        public bool ContinuationFlag { get; set; }
    }

}
