using System;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
    public class DocumentContainerInfo
    {
        public string Id { get; set; }
        public string PartitionKey { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
