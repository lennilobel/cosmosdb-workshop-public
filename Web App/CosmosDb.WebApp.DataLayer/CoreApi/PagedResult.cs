using System.Collections.Generic;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
    public class PagedResult
    {
        public IEnumerable<object> Data { get; set; }
        public string ContinuationToken { get; set; }
    }
}
