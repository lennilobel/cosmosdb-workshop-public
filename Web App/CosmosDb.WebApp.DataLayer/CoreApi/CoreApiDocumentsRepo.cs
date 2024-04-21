using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
	// !!! Add stream queries in SDK3
	public static class CoreApiDocumentsRepo
    {
        public static async Task<object> CreateDocumentFromDynamic(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            dynamic documentDefinition = new
            {
				id = Guid.NewGuid().ToString(),
                name = "New Customer 1",
                address = new
                {
                    addressType = "Main Office",
                    addressLine1 = "123 Main Street",
                    location = new
                    {
                        city = "Brooklyn",
                        stateProvinceName = "New York"
                    },
                    postalCode = "11229",
                    countryRegionName = "United States"
                },
            };

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);
				var result = await container.CreateItemAsync(documentDefinition, new PartitionKey(documentDefinition.address.postalCode));
                var document = result.Resource;

                return document;
            }
        }

        public static async Task<object> CreateDocumentFromJson(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var documentDefinition = $@"
			{{
				""id"": ""{Guid.NewGuid()}"",
				""name"": ""New Customer 2"",
				""address"": {{
					""addressType"": ""Main Office"",
					""addressLine1"": ""123 Main Street"",
					""location"": {{
						""city"": ""Brooklyn"",
						""stateProvinceName"": ""New York""
					}},
					""postalCode"": ""11229"",
					""countryRegionName"": ""United States""
				}}
			}}";

			var documentObject = JsonConvert.DeserializeObject<JObject>(documentDefinition);
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);
				var result = await container.CreateItemAsync(documentObject, new PartitionKey(documentObject["address"]["postalCode"].Value<string>()));
				var document = result.Resource;

				return document;
			}
        }

		public static async Task<object> CreateDocumentFromPoco(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var documentDefinition = new Customer
            {
				Id = Guid.NewGuid().ToString(),
                Name = "New Customer 3",
                Address = new Address
                {
                    AddressType = "Main Office",
                    AddressLine1 = "123 Main Street",
                    Location = new Location
                    {
                        City = "Brooklyn",
                        StateProvinceName = "New York"
                    },
                    PostalCode = "11229",
                    CountryRegionName = "United States"
                },
            };

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);
				var result = await container.CreateItemAsync(documentDefinition, new PartitionKey(documentDefinition.Address.PostalCode));
				var document = result.Resource;

				return document;
			}
        }

        public static async Task<IEnumerable<object>> QueryForDynamic(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var results = new List<object>();

			var sql = "SELECT * FROM c WHERE STARTSWITH(c.name, @NamePrefix) = true";
			var queryDefinition = new QueryDefinition(sql)
				.WithParameter("@NamePrefix", "New Customer");

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);
				var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);
				while (iterator.HasMoreResults)
				{
					var documents = await iterator.ReadNextAsync();
					foreach (var document in documents)
					{
						Customer customer = JsonConvert.DeserializeObject<Customer>(document.ToString());
						dynamic item = new
						{
							Id = document.id,
							Name = document.name,
							City = customer.Address.Location.City
						};
						results.Add(item);
					}
				}
			}

			return results;
        }

		public static async Task<IEnumerable<object>> QueryForPoco(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var results = new List<object>();

            var sql = "SELECT * FROM c WHERE STARTSWITH(c.name, 'New Customer') = true";
			var queryDefinition = new QueryDefinition(sql);

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);
				var iterator = container.GetItemQueryIterator<Customer>(queryDefinition);
				while (iterator.HasMoreResults)
				{
					var documents = await iterator.ReadNextAsync();
					foreach (var customer in documents)
					{
						dynamic item = new
						{
							customer.Id,
							customer.Name,
							customer.Address.Location.City
						};
						results.Add(item);
					}
				}
			}

            return results;
        }

        public static IEnumerable<object> QueryWithLinq(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
            {
                var container = client.GetContainer(databaseId, containerId);

                var q = from d in container.GetItemLinqQueryable<Customer>(allowSynchronousQueryExecution: true)
                        where d.Address.CountryRegionName == "United Kingdom"
                        select new
                        {
                            d.Id,
                            d.Name,
                            d.Address.Location.City
                        };

                var documents = q.ToList();

                return documents;
            }
        }

        public static async Task<PagedResult> QueryWithPaging(AppConfig config, string databaseId, string containerId, object previousContinuationToken = null)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var results = new List<object>();

			var sql = "SELECT * FROM c";

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);

				var continuationTokenString = default(string);
				if (previousContinuationToken != null)
				{
					continuationTokenString = previousContinuationToken.ToString();
				}

				var options = new QueryRequestOptions
				{
					MaxItemCount = 100,
				};

				var query = container.GetItemQueryIterator<Customer>(sql, continuationTokenString, options);

				var documents = await query.ReadNextAsync();
				var continuationToken = documents.ContinuationToken;
				results.AddRange(documents.Select(d => new
				{
					d.Id,
					d.Name,
					d.Address.Location.City
				}));

				var pagedResult = new PagedResult
				{
					Data = results,
					ContinuationToken = continuationToken
				};

				return pagedResult;
			}
		}

        public static async Task<string> ReplaceDocuments(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var sb = new StringBuilder();
            sb.AppendLine(">>> Replace Documents <<<");
            sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer(databaseId, containerId);

				sb.AppendLine("Querying WHERE c.isNew = true");
                var sql = "SELECT VALUE COUNT(c) FROM c WHERE c.isNew = true";
				var count = (await (container.GetItemQueryIterator<int>(sql)).ReadNextAsync()).First();
				sb.AppendLine($"Documents with 'isNew' flag: {count}");
                sb.AppendLine();

                sb.AppendLine("Querying WHERE STARTSWITH(c.name, 'New Customer') = true");
                sql = "SELECT * FROM c WHERE STARTSWITH(c.name, 'New Customer') = true";
				var documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();
				sb.AppendLine($"Found {documents.Count} documents to be updated");
                foreach (var document in documents)
                {
					document.Add("isNew", true);
					var result = await container.ReplaceItemAsync<dynamic>(document, (string)document.id);
                    var updatedDocument = result.Resource;
					sb.AppendLine($"Updated document ID {updatedDocument["id"]} 'isNew' flag: {updatedDocument["isNew"]}");
				}
				sb.AppendLine();

                sb.AppendLine("Querying WHERE c.isNew = true");
                sql = "SELECT VALUE COUNT(c) FROM c WHERE c.isNew = true";
				count = (await (container.GetItemQueryIterator<int>(sql)).ReadNextAsync()).First();
				sb.AppendLine($"Documents with 'isNew' flag: {count}");
            }

            return sb.ToString();
        }

		public static async Task<string> DeleteDocuments(AppConfig config, string databaseId, string containerId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var sb = new StringBuilder();
            sb.AppendLine(">>> Delete Documents <<<");
            sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
            {
				var container = client.GetContainer(databaseId, containerId);

				sb.AppendLine("Querying WHERE STARTSWITH(c.name, 'New Customer') = true");
				var sql = "SELECT c.id, c.address.postalCode FROM c WHERE STARTSWITH(c.name, 'New Customer') = true";
				var iterator = container.GetItemQueryIterator<dynamic>(sql);
				var documents = (await iterator.ReadNextAsync()).ToList();
				sb.AppendLine($"Found {documents.Count} documents to be deleted");
				foreach (var document in documents)
				{
					string id = document.id;
					string pk = document.postalCode;
					await container.DeleteItemAsync<dynamic>(id, new PartitionKey(pk));
					sb.AppendLine($"Deleted document with ID '{document["id"]}' (partition key '{document["postalCode"]}')");
				}

				sb.AppendLine($"Deleted {documents.Count} new customer documents");
            }

            return sb.ToString();
        }
    }

}
