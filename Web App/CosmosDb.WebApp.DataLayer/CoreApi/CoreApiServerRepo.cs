using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
    public static class CoreApiServerRepo
    {
        #region "Stored Procedures"

        public static async Task<IEnumerable<StoredProcedureProperties>> GetStoredProcedures(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sprocs = new List<StoredProcedureProperties>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var iterator = client.GetContainer("mydb", "mystore").Scripts.GetStoredProcedureQueryIterator<StoredProcedureProperties>();
				while (iterator.HasMoreResults)
				{
					sprocs.AddRange(await iterator.ReadNextAsync());
				}
			}

			return sprocs.OrderBy(sp => sp.Id);
		}

		public static async Task<string> CreateStoredProcedures(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var sb = new StringBuilder();

            sb.AppendLine(">>> Create Stored Procedures <<<");
            sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await CreateStoredProcedure(client, sb, "spHelloWorld");
                await CreateStoredProcedure(client, sb, "spSetNorthAmerica");
                await CreateStoredProcedure(client, sb, "spGenerateId");
                await CreateStoredProcedure(client, sb, "spBulkInsert");
                await CreateStoredProcedure(client, sb, "spBulkDelete");
            }

            return sb.ToString();
        }

        private static async Task<StoredProcedureProperties> CreateStoredProcedure(CosmosClient client, StringBuilder sb, string sprocId)
        {
            var sprocBody = File.ReadAllText($@".\Files\CoreApi\Server\{sprocId}.js");

			var sprocProperties = new StoredProcedureProperties
			{
				Id = sprocId,
				Body = sprocBody,
			};

			var container = client.GetContainer("mydb", "mystore");
			var result = await container.Scripts.CreateStoredProcedureAsync(sprocProperties);
            var sproc = result.Resource;

			sb.AppendLine($"Created stored procedure {sproc.Id}");

			return result;
        }

        public static async Task<string> ExecuteStoredProcedure(AppConfig config, string sprocId)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var sb = new StringBuilder();

            sb.AppendLine($">>> Execute Stored Procedure {sprocId} <<<");
            sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				switch (sprocId)
                {
                    case "spHelloWorld":
                        await Execute_spHelloWorld(client, sb);
                        break;

                    case "spSetNorthAmerica":
                        await Execute_spSetNorthAmerica1(client, sb);
                        await Execute_spSetNorthAmerica2(client, sb);
                        await Execute_spSetNorthAmerica3(client, sb);
                        break;

                    case "spGenerateId":
                        await Execute_spGenerateId(client, sb);
                        break;

                    case "spBulkInsert":
                        await Execute_spBulkInsert(client, sb);
                        break;

                    case "spBulkDelete":
                        await Execute_spBulkDelete(client, sb);
                        break;
                }
            }

            return sb.ToString();
        }

        private static async Task Execute_spHelloWorld(CosmosClient client, StringBuilder sb)
        {
            sb.AppendLine("Execute spHelloWorld stored procedure");

            var scripts = client.GetContainer("mydb", "mystore").Scripts;
            var pk = new PartitionKey(string.Empty);
            var result = await scripts.ExecuteStoredProcedureAsync<string>("spHelloWorld", pk, null);
            var message = result.Resource;

            sb.AppendLine($"Result: {message}");
        }

        private static async Task Execute_spSetNorthAmerica1(CosmosClient client, StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("Execute spSetNorthAmerica (country = United States)");

            // Should succeed with isNorthAmerica = true
            var id = Guid.NewGuid().ToString();
            dynamic documentDefinition = new
            {
                id,
                name = "John Doe",
                address = new
                {
                    countryRegionName = "United States",
                    postalCode = "12345"
                }
            };

            var container = client.GetContainer("mydb", "mystore");
            var pk = new PartitionKey(documentDefinition.address.postalCode);
            var result = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spSetNorthAmerica", pk, new[] { documentDefinition, true });
            var document = result.Resource;

            var country = document.address.countryRegionName;
            var isNA = document.address.isNorthAmerica;

            sb.AppendLine("Result:");
            sb.AppendLine($" Country = {country}");
            sb.AppendLine($" Is North America = {isNA}");

            await container.DeleteItemAsync<dynamic>(id, pk);
        }

        private static async Task Execute_spSetNorthAmerica2(CosmosClient client, StringBuilder sb)
        {
			sb.AppendLine();
			sb.AppendLine("Execute spSetNorthAmerica (country = United Kingdom)");

            // Should succeed with isNorthAmerica = false
            var id = Guid.NewGuid().ToString();
            dynamic documentDefinition = new
            {
                id,
                name = "John Doe",
                address = new
                {
                    countryRegionName = "United Kingdom",
                    postalCode = "RG41 1QW"
                }
            };

            var container = client.GetContainer("mydb", "mystore");
            var pk = new PartitionKey(documentDefinition.address.postalCode);
            var result = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spSetNorthAmerica", pk, new[] { documentDefinition, true });
            var document = result.Resource;

            // Deserialize new document as JObject (use dictionary-style indexers to access dynamic properties)
            var documentObject = JsonConvert.DeserializeObject(document.ToString());

            var country = documentObject["address"]["countryRegionName"];
            var isNA = documentObject["address"]["isNorthAmerica"];

            sb.AppendLine("Result:");
            sb.AppendLine($" Country = {country}");
            sb.AppendLine($" Is North America = {isNA}");

            await container.DeleteItemAsync<dynamic>(id, pk);
        }

        private static async Task Execute_spSetNorthAmerica3(CosmosClient client, StringBuilder sb)
        {
			sb.AppendLine();
			sb.AppendLine("Execute spSetNorthAmerica (no country)");

            var id = Guid.NewGuid().ToString();
            dynamic documentDefinition = new
            {
                id,
                name = "James Smith",
                address = new
                {
                    postalCode = "12345"
                }
            };

            var container = client.GetContainer("mydb", "mystore");
            var pk = new PartitionKey(documentDefinition.address.postalCode);

            try
            {
                // Should fail with no country and enforceSchema = true
                var result = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spSetNorthAmerica", pk, new[] { documentDefinition, true });
            }
            catch (CosmosException ex)
            {
                sb.AppendLine($"Error: {ex.Message}");
            }
        }

        private static async Task Execute_spGenerateId(CosmosClient client, StringBuilder sb)
        {
			sb.AppendLine("Execute spGenerateId");

            dynamic documentDefinition1 = new
            {
                firstName = "Albert",
                lastName = "Einstein",
                address = new { postalCode = "12345" }
            };

            dynamic documentDefinition2 = new
            {
                firstName = "Alfred",
                lastName = "Einstein",
                address = new { postalCode = "12345" }
            };

            dynamic documentDefinition3 = new
            {
                firstName = "Ashton",
                lastName = "Einstein",
                address = new { postalCode = "12345" }
            };

            dynamic documentDefinition4 = new
            {
                firstName = "Albert",
                lastName = "Einstein",
                address = new { postalCode = "54321" }
            };

            var container = client.GetContainer("mydb", "mystore");
            var pk12345 = new PartitionKey("12345");
            var pk54321 = new PartitionKey("54321");

            var result1 = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spGenerateId", pk12345, new[] { documentDefinition1 });
            var document1 = result1.Resource;
            sb.AppendLine($"New document in PK '{document1.address.postalCode}', generated ID '{document1.id}' for '{document1.firstName} {document1.lastName}'");

            var result2 = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spGenerateId", pk12345, new[] { documentDefinition2 });
            var document2 = result2.Resource;
            sb.AppendLine($"New document in PK '{document2.address.postalCode}', generated ID '{document2.id}' for '{document2.firstName} {document2.lastName}'");

            var result3 = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spGenerateId", pk12345, new[] { documentDefinition3 });
            var document3 = result3.Resource;
            sb.AppendLine($"New document in PK '{document3.address.postalCode}', generated ID '{document3.id}' for '{document3.firstName} {document3.lastName}'");

            var result4 = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spGenerateId", pk54321, new[] { documentDefinition4 });
            var document4 = result4.Resource;
            sb.AppendLine($"New document in PK '{document4.address.postalCode}', generated ID '{document4.id}' for '{document4.firstName} {document4.lastName}'");

            // cleanup
            await container.DeleteItemAsync<dynamic>(document1.id.ToString(), pk12345);
            await container.DeleteItemAsync<dynamic>(document2.id.ToString(), pk12345);
            await container.DeleteItemAsync<dynamic>(document3.id.ToString(), pk12345);
            await container.DeleteItemAsync<dynamic>(document4.id.ToString(), pk54321);
        }

        private static async Task Execute_spBulkInsert(CosmosClient client, StringBuilder sb)
        {
			sb.AppendLine("Execute spBulkInsert");
			sb.AppendLine();

			var docs = new List<dynamic>();
			var total = 5000;
			for (var i = 1; i <= total; i++)
			{
				dynamic doc = new
				{
					name = $"Bulk inserted doc {i}",
					address = new
					{
						postalCode = "12345"
					}
				};
				docs.Add(doc);
			}

            var container = client.GetContainer("mydb", "mystore");
            var pk = new PartitionKey("12345");
            var started = DateTime.Now;
            var totalInserted = 0;
            while (totalInserted < total)
            {
                var callStarted = DateTime.Now;
                var result = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spBulkInsert", pk, new[] { docs });
                var callElapsed = DateTime.Now.Subtract(callStarted);
                var inserted = (int)result.Resource;
                totalInserted += inserted;
                var remaining = total - totalInserted;
                sb.AppendLine($"Inserted {inserted} documents ({totalInserted} total, {remaining} remaining); elapsed: {callElapsed}");
                docs = docs.GetRange(inserted, docs.Count - inserted);
            }
            var elapsed = DateTime.Now.Subtract(started);
            sb.AppendLine();
            sb.AppendLine($"Overall elapsed: {elapsed}");
        }

		private static async Task Execute_spBulkDelete(CosmosClient client, StringBuilder sb)
        {
			sb.AppendLine("Execute spBulkDelete");
			sb.AppendLine();

			// query retrieves self-links for documents to bulk-delete
			var whereClause = "STARTSWITH(c.name, 'Bulk inserted doc ') = true";
			var count = await Execute_spBulkDelete(client, sb, whereClause);
            sb.AppendLine($"Deleted bulk inserted documents; count: {count}");
            sb.AppendLine();
        }

		private static async Task<int> Execute_spBulkDelete(CosmosClient client, StringBuilder sb, string sql)
        {
            var container = client.GetContainer("mydb", "mystore");
            var pk = new PartitionKey("12345");
            var continuationFlag = true;
            var totalDeleted = 0;
            var started = DateTime.Now;
            while (continuationFlag)
            {
            	var callStarted = DateTime.Now;
                var result = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spBulkDelete", pk, new[] { sql });
                var callElapsed = DateTime.Now.Subtract(callStarted);
                var response = result.Resource;
                continuationFlag = response.continuationFlag;
                var deleted = response.count.Value;
                totalDeleted += deleted;
                sb.AppendLine($"Deleted {deleted} documents ({totalDeleted} total, more: {continuationFlag}); elapsed: {callElapsed}");
            }
            var elapsed = DateTime.Now.Subtract(started);
            sb.AppendLine();
            sb.AppendLine($"Overall elapsed: {elapsed}");

            return totalDeleted;
		}

		public static async Task<string> DeleteStoredProcedures(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var sb = new StringBuilder();

            sb.AppendLine(">>> Delete Stored Procedures <<<");
            sb.AppendLine();

            using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
            {
				await TryDeleteStoredProcedure(client, sb, "spHelloWorld");
                await TryDeleteStoredProcedure(client, sb, "spSetNorthAmerica");
                await TryDeleteStoredProcedure(client, sb, "spGenerateId");
                await TryDeleteStoredProcedure(client, sb, "spBulkInsert");
                await TryDeleteStoredProcedure(client, sb, "spBulkDelete");
            }

            return sb.ToString();
        }

        private static async Task TryDeleteStoredProcedure(CosmosClient client, StringBuilder sb, string sprocId)
        {
            var container = client.GetContainer("mydb", "mystore");
            await container.Scripts.DeleteStoredProcedureAsync(sprocId);

            sb.AppendLine($"Deleted stored procedure: {sprocId}");
        }

		#endregion

		#region "Triggers"

		public static async Task<IEnumerable<TriggerProperties>> GetTriggers(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var triggers = new List<TriggerProperties>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var iterator = client.GetContainer("mydb", "mystore").Scripts.GetTriggerQueryIterator<TriggerProperties>();
				while (iterator.HasMoreResults)
				{
					triggers.AddRange(await iterator.ReadNextAsync());
				}
			}

			return triggers.OrderBy(t => t.Id);
		}

		public static async Task<string> CreateTriggers(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine(">>> Create Triggers <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await CreateTrigger(client, sb, "trgValidateDocument", TriggerType.Pre, TriggerOperation.All);
				await CreateTrigger(client, sb, "trgUpdateMetadata", TriggerType.Post, TriggerOperation.Create);
			}

			return sb.ToString();
		}

		private static async Task<TriggerProperties> CreateTrigger(CosmosClient client, StringBuilder sb, string triggerId, TriggerType triggerType, TriggerOperation triggerOperation)
		{
			var triggerBody = File.ReadAllText($@".\Files\CoreApi\Server\{triggerId}.js");

			var triggerProperties = new TriggerProperties
			{
				Id = triggerId,
				Body = triggerBody,
				TriggerType = triggerType,
				TriggerOperation = triggerOperation,
			};

			var container = client.GetContainer("mydb", "mystore");
			var result = await container.Scripts.CreateTriggerAsync(triggerProperties);
			var trigger = result.Resource;

			sb.AppendLine($"Created trigger {trigger.Id}");

			return result;
		}

		public static async Task<string> ExecuteTrigger(AppConfig config, string triggerId)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine($">>> Execute Trigger {triggerId} <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				switch (triggerId)
				{
					case "trgValidateDocument":
						await Execute_trgValidateDocument(client, sb);
						break;

					case "trgUpdateMetadata":
						await Execute_trgUpdateMetadata(client, sb);
						break;
				}
			}

			return sb.ToString();
		}

		private static async Task Execute_trgValidateDocument(CosmosClient client, StringBuilder sb)
		{
			// Create three documents
			var doc1Id = await CreateDocumentWithValidation(client, sb, "mon");       // Monday
			var doc2Id = await CreateDocumentWithValidation(client, sb, "THURS");     // Thursday
			var doc3Id = await CreateDocumentWithValidation(client, sb, "sonday");    // error - won't get created

			// Update one of them
			await UpdateDocumentWithValidation(client, sb, doc2Id, "FRI");            // Thursday > Friday

			// Delete them
			var container = client.GetContainer("mydb", "mystore");
			var pk = new PartitionKey("12345");
			await container.DeleteItemAsync<dynamic>(doc1Id, pk);
			await container.DeleteItemAsync<dynamic>(doc2Id, pk);
		}

		private static async Task<string> CreateDocumentWithValidation(CosmosClient client, StringBuilder sb, string weekdayOff)
		{
			dynamic document = new
			{
				id = Guid.NewGuid().ToString(),
				name = "John Doe",
				address = new { postalCode = "12345" },
				weekdayOff
			};

			var container = client.GetContainer("mydb", "mystore");
			var pk = new PartitionKey("12345");

			try
			{
				var options = new ItemRequestOptions { PreTriggers = new[] { "trgValidateDocument" } };
				var result = await container.CreateItemAsync<dynamic>(document, pk, options);
				document = result.Resource;

				sb.AppendLine(" Result:");
				sb.AppendLine($"  Id = {document.id}");
				sb.AppendLine($"  Weekday off = {document.weekdayOff}");
				sb.AppendLine($"  Weekday # off = {document.weekdayNumberOff}");
				sb.AppendLine();

				return document.id;
			}
			catch (CosmosException ex)
			{
				sb.AppendLine($"Error: {ex.Message}");
				sb.AppendLine();

				return null;
			}
		}

		private static async Task UpdateDocumentWithValidation(CosmosClient client, StringBuilder sb, string documentId, string weekdayOff)
		{
			var sql = $"SELECT * FROM c WHERE c.id = '{documentId}'";
			var container = client.GetContainer("mydb", "mystore");
			var document = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).FirstOrDefault();

			document.weekdayOff = weekdayOff;

			var pk = new PartitionKey("12345");
			var options = new ItemRequestOptions { PreTriggers = new[] { "trgValidateDocument" } };
			var result = await container.ReplaceItemAsync(document, documentId, pk, options);
			document = result.Resource;

			sb.AppendLine(" Result:");
			sb.AppendLine($"  Id = {document.id}");
			sb.AppendLine($"  Weekday off = {document.weekdayOff}");
			sb.AppendLine($"  Weekday # off = {document.weekdayNumberOff}");
			sb.AppendLine();
		}

		private static async Task Execute_trgUpdateMetadata(CosmosClient client, StringBuilder sb)
		{
			// Show no metadata documents
			await ViewMetaDocs(client, sb);

			// Create a bunch of documents across two partition keys
			var docs = new List<dynamic>
			{
				// 11229
				new { id = "11229a", address = new { postalCode = "11229" }, name = "New Customer ABCD" },
				new { id = "11229b", address = new { postalCode = "11229" }, name = "New Customer ABC" },
				new { id = "11229c", address = new { postalCode = "11229" }, name = "New Customer AB" },			// smallest
				new { id = "11229d", address = new { postalCode = "11229" }, name = "New Customer ABCDEF" },
				new { id = "11229e", address = new { postalCode = "11229" }, name = "New Customer ABCDEFG" },		// largest
				new { id = "11229f", address = new { postalCode = "11229" }, name = "New Customer ABCDE" },
				// 11235
				new { id = "11235a", address = new { postalCode = "11235" }, name = "New Customer AB" },
				new { id = "11235b", address = new { postalCode = "11235" }, name = "New Customer ABCDEFGHIJKL" },	// largest
				new { id = "11235c", address = new { postalCode = "11235" }, name = "New Customer ABC" },
				new { id = "11235d", address = new { postalCode = "11235" }, name = "New Customer A" },				// smallest
				new { id = "11235e", address = new { postalCode = "11235" }, name = "New Customer ABC" },
				new { id = "11235f", address = new { postalCode = "11235" }, name = "New Customer ABCDE" },
			};

			var container = client.GetContainer("mydb", "mystore");
			foreach (var doc in docs)
			{
				var pk = new PartitionKey(doc.address.postalCode);
				var options = new ItemRequestOptions { PostTriggers = new[] { "trgUpdateMetadata" } };
				var result = await container.CreateItemAsync(doc, pk, options);
			}

			// Show two metadata documents
			await ViewMetaDocs(client, sb);

			// Cleanup
			var sql = @"
				SELECT c.id, c.address.postalCode
				FROM c
				WHERE c.address.postalCode IN('11229', '11235')
			";

			var documentIds = await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync();
			foreach (var documentKey in documentIds)
			{
				var id = documentKey.id.Value;
				var pk = documentKey.postalCode.Value;

				await container.DeleteItemAsync<dynamic>(id, new PartitionKey(pk));
			}
		}

		private static async Task ViewMetaDocs(CosmosClient client, StringBuilder sb)
		{
			var sql = @"SELECT * FROM c WHERE c.isMetaDoc";

			var container = client.GetContainer("mydb", "mystore");
			var metaDocs = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();

			sb.AppendLine();
			sb.AppendLine($" Found {metaDocs.Count} metadata documents:");
			foreach (var metaDoc in metaDocs)
			{
				sb.AppendLine();
				sb.AppendLine($"  MetaDoc ID: {metaDoc.id}");
				sb.AppendLine($"  Metadata for: {metaDoc.address.postalCode}");
				sb.AppendLine($"  Smallest doc size: {metaDoc.minSize} ({metaDoc.minSizeId})");
				sb.AppendLine($"  Largest doc size: {metaDoc.maxSize} ({metaDoc.maxSizeId})");
				sb.AppendLine($"  Total doc size: {metaDoc.totalSize}");
			}
		}

		public static async Task<string> DeleteTriggers(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine(">>> Delete Triggers <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await TryDeleteTrigger(client, sb, "trgValidateDocument");
				await TryDeleteTrigger(client, sb, "trgUpdateMetadata");
			}

			return sb.ToString();
		}

		private static async Task TryDeleteTrigger(CosmosClient client, StringBuilder sb, string triggerId)
		{
			var container = client.GetContainer("mydb", "mystore");
			await container.Scripts.DeleteTriggerAsync(triggerId);

			sb.AppendLine($"Deleted trigger: {triggerId}");
		}

		#endregion

		#region "User-Defined Functions"

		public static async Task<IEnumerable<UserDefinedFunctionProperties>> GetUserDefinedFunctions(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var udfs = new List<UserDefinedFunctionProperties>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var iterator = client.GetContainer("mydb", "mystore").Scripts.GetUserDefinedFunctionQueryIterator<UserDefinedFunctionProperties>();
				while (iterator.HasMoreResults)
				{
					udfs.AddRange(await iterator.ReadNextAsync());
				}
			}

			return udfs.OrderBy(t => t.Id);
		}

		public static async Task<string> CreateUserDefinedFunctions(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine(">>> Create UDFs <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await CreateUserDefinedFunction(client, sb, "udfRegEx");
				await CreateUserDefinedFunction(client, sb, "udfIsNorthAmerica");
				await CreateUserDefinedFunction(client, sb, "udfFormatCityStateZip");
			}

			return sb.ToString();
		}

		private static async Task<UserDefinedFunctionProperties> CreateUserDefinedFunction(CosmosClient client, StringBuilder sb, string udfId)
		{
			var udfBody = File.ReadAllText($@".\Files\CoreApi\Server\{udfId}.js");

			var udfProperties = new UserDefinedFunctionProperties
			{
				Id = udfId,
				Body = udfBody,
			};

			var container = client.GetContainer("mydb", "mystore");
			var result = await container.Scripts.CreateUserDefinedFunctionAsync(udfProperties);
			var sproc = result.Resource;

			sb.AppendLine($"Created user-defined function {sproc.Id}");

			return result;
		}

		public static async Task<string> ExecuteUserDefinedFunction(AppConfig config, string udfId)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine($">>> Execute UDF {udfId} <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				switch (udfId)
				{
					case "spHelloWorld":
						await Execute_udfRegEx(client, sb);
						break;

					case "spSetNorthAmerica":
						await Execute_udfIsNorthAmerica(client, sb);
						break;

					case "spGenerateId":
						await Execute_udfFormatCityStateZip(client, sb);
						break;
				}
			}

			return sb.ToString();
		}

		private async static Task Execute_udfRegEx(CosmosClient client, StringBuilder sb)
		{
			Console.Clear();
			sb.AppendLine("Querying for Rental customers");

			var sql = "SELECT c.id, c.name FROM c WHERE udf.udfRegEx(c.name, 'Rental') != null";

			var container = client.GetContainer("mydb", "mystore");
			var documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();

			sb.AppendLine($"Found {documents.Count} Rental customers:");
			foreach (var document in documents)
			{
				sb.AppendLine($" {document.name} ({document.id})");
			}
		}

		private async static Task Execute_udfIsNorthAmerica(CosmosClient client, StringBuilder sb)
		{
			sb.AppendLine("Querying for North American customers");

			var sql = @"
				SELECT c.name, c.address.countryRegionName
				FROM c
				WHERE udf.udfIsNorthAmerica(c.address.countryRegionName) = true";

			var container = client.GetContainer("mydb", "mystore");
			var documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();

			sb.AppendLine($"Found {documents.Count} North American customers; first 20:");
			foreach (var document in documents.Take(20))
			{
				sb.AppendLine($" {document.name}, {document.countryRegionName}");
			}

			sql = @"
				SELECT c.name, c.address.countryRegionName
				FROM c
				WHERE udf.udfIsNorthAmerica(c.address.countryRegionName) = false";

			sb.AppendLine();
			sb.AppendLine("Querying for non North American customers");

			documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();

			sb.AppendLine($"Found {documents.Count} non North American customers; first 20:");
			foreach (var document in documents.Take(20))
			{
				sb.AppendLine($" {document.name}, {document.countryRegionName}");
			}
		}

		private async static Task Execute_udfFormatCityStateZip(CosmosClient client, StringBuilder sb)
		{
			sb.AppendLine();
			sb.AppendLine("Listing names with city, state, zip (first 20)");

			var sql = "SELECT c.name, udf.udfFormatCityStateZip(c) AS csz FROM c";

			var container = client.GetContainer("mydb", "mystore");
			var documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();
			foreach (var document in documents.Take(20))
			{
				sb.AppendLine($" {document.name} located in {document.csz}");
			}
		}

		public static async Task<string> DeleteUserDefinedFunctions(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine(">>> Delete UDFs <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await TryDeleteUserDefinedFunction(client, sb, "udfRegEx");
				await TryDeleteUserDefinedFunction(client, sb, "udfIsNorthAmerica");
				await TryDeleteUserDefinedFunction(client, sb, "udfFormatCityStateZip");
			}

			return sb.ToString();
		}

		private static async Task TryDeleteUserDefinedFunction(CosmosClient client, StringBuilder sb, string udfId)
		{
			var container = client.GetContainer("mydb", "mystore");
			await container.Scripts.DeleteUserDefinedFunctionAsync(udfId);

			sb.AppendLine($"Deleted user-defined function: {udfId}");
		}

		#endregion

	}
}
