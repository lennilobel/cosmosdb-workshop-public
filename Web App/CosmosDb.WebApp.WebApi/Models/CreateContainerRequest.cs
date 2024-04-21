namespace CosmosDb.WebApp.WebApi.Models
{
    public class CreateContainerRequest
    {
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public string PartitionKey { get; set; }
        public int Throughput { get; set; }
    }

}
