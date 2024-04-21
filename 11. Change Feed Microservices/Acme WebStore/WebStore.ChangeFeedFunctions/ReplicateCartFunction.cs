using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace WebStore.ChangeFeedFunctions
{
	class ReplicateCartFunction
	{
		private static readonly CosmosClient _client;

		static ReplicateCartFunction()
		{
			var connStr = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
			_client = new CosmosClient(connStr);
		}

		[FunctionName("ReplicateCart")]
		public static async Task ReplicateCart(
			[CosmosDBTrigger(
				databaseName: "acme-webstore",
				containerName: "cart",
				Connection = "CosmosDbConnectionString",
				LeaseContainerName = "lease",
				LeaseContainerPrefix = "ReplicateCart"
			)]
			string documentsJson,
			ILogger logger)
		{
			var container = _client.GetContainer("acme-webstore", "cartByItem");
			var documents = JsonConvert.DeserializeObject<JArray>(documentsJson);
			foreach (JObject document in documents)
			{
				try
				{
					if (document["ttl"] == null)
					{
						await container.UpsertItemAsync(document);
						logger.LogWarning($"Upserted document id {document["id"]} in replica container");
					}
					else
					{
						var id = document["id"].Value<string>();
						var item = document["item"].Value<string>();
						await container.DeleteItemAsync<JObject>(id, new PartitionKey(item));
						logger.LogWarning($"Deleted document id {document["id"]} in replica container");
					}
				}
				catch (Exception ex)
				{
					logger.LogError($"Error processing change for document id {document["id"]}: {ex.Message}");
				}
			}
		}

	}
}
