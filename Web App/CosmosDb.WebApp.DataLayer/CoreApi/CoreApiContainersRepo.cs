using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
    public static class CoreApiContainersRepo
    {
		public static async Task<IEnumerable<DocumentContainerInfo>> GetContainers(AppConfig config, string databaseId)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var result = new List<DocumentContainerInfo>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
                var iterator = client.GetDatabase(databaseId).GetContainerQueryIterator<ContainerProperties>();
                var containers = await iterator.ReadNextAsync();

                var count = 0;
                foreach (var container in containers)
                {
                    count++;
                    var dci = new DocumentContainerInfo
                    {
                        Id = container.Id,
                        PartitionKey = container.PartitionKeyPath,
                        LastModified = container.LastModified,
                    };
                    result.Add(dci);
                }

				return result;
			}
		}

        public static async Task<ContainerProperties> CreateContainer(AppConfig config, string databaseId, string containerId, string partitionKey, int throughput)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var containerProperties = new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = partitionKey,
            };

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
            {
                var result = await client.GetDatabase(databaseId).CreateContainerAsync(containerProperties, throughput);
                var container = result.Resource;
            }

            return containerProperties;
        }

        public static async Task DeleteContainer(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
                await client.GetContainer(databaseId, containerId).DeleteContainerAsync();
			}
        }

	}
}
