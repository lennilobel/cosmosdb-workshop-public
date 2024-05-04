using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace CosmosDb.ResourceTokens.Demos
{
	public static class PartitionKeyPermissionsDemo
	{
		public async static Task Run()
		{
			Debugger.Break();

			// Delete the database if it exists
			var database = Shared.Client.GetDatabase("multi-tenant");
			try
			{
				await database.DeleteAsync();
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
			}

			// Create the database
			await Shared.Client.CreateDatabaseAsync("multi-tenant");
			database = Shared.Client.GetDatabase("multi-tenant");

			// Create the container, partitioned on the tenantId
			await database.CreateContainerAsync("tenant-data", "/tenantId", 400);
			var container = database.GetContainer("tenant-data");

			// Create one user per tenant
			await database.CreateUserAsync("Acme");
			await database.CreateUserAsync("Wonka");

			// Create AcmePartition permission for Acme user to only access items in the Acme partition
			var acmeUser = database.GetUser("Acme");
			var acmePermissionProps = new PermissionProperties("AcmePartition", PermissionMode.All, container, new PartitionKey("Acme"));
			var acmePermission = (await acmeUser.CreatePermissionAsync(acmePermissionProps)).Resource;

			// Create WonkaPartition permission for Wonka user to only access items in the Wonka partition 
			var wonkaUser = database.GetUser("Wonka");
			var wonkaPermissionProps = new PermissionProperties("WonkaPartition", PermissionMode.All, container, new PartitionKey("Wonka"));
			var wonkaPermission = (await wonkaUser.CreatePermissionAsync(wonkaPermissionProps)).Resource;

			// Define two documents; one for each tenant
			dynamic acmeDoc = new { id = "AcmeItem1", tenantId = "Acme", description = "Acme Item 1" };
			dynamic wonkaDoc = new { id = "WonkaItem1", tenantId = "Wonka", description = "Wonka Item 1" };

			// Try to create both documents using the resource token for Acme
			Console.WriteLine();
			Console.WriteLine(">>> Attempting to create documents using the Acme resource token <<<");
			await CreateDocument(acmePermission.Token, acmeDoc);    // Acme can create an Acme document
			await CreateDocument(acmePermission.Token, wonkaDoc);   // Acme cannot create a Wonka document

			// Try to create both documents using the resource token for Wonka
			Console.WriteLine();
			Console.WriteLine(">>> Attempting to create documents using the Wonka resource token <<<");
			await CreateDocument(wonkaPermission.Token, acmeDoc);	// Wonka cannot create an Acme document
			await CreateDocument(wonkaPermission.Token, wonkaDoc);	// Wonka can create a Wonka document

			// Try to query for all documents in a tenant's partition
			Console.WriteLine();
			Console.WriteLine(">>> Attempting to query for all documents in a partition <<<");
			await QueryDocuments(acmePermission.Token, "Acme");		// Acme can retrieve all the Acme documents
			await QueryDocuments(acmePermission.Token, "Wonka");	// Acme cannot retrieve any of the Wonka documents
			await QueryDocuments(wonkaPermission.Token, "Acme");	// Wonka cannot retrieve any of the Acme documents
			await QueryDocuments(wonkaPermission.Token, "Wonka");   // Wonka can retrieve all the WWonka documents

			// Try to retrieve a single documents from a tenant's partition
			Console.WriteLine();
			Console.WriteLine(">>> Attempting to read a single document in a partition <<<");
			await ReadDocument(acmePermission.Token, "Acme", "AcmeItem1");		// Acme can retrieve an Acme document
			await ReadDocument(acmePermission.Token, "Wonka", "WonkaItem1");	// Acme cannot retrieve a Wonka document
			await ReadDocument(wonkaPermission.Token, "Acme", "AcmeItem1");		// Wonka cannot retrieve an Acme document
			await ReadDocument(wonkaPermission.Token, "Wonka", "WonkaItem1");	// Wonka can retrieve a Wonka document

			// Delete the database
			await database.DeleteAsync();
		}

		private async static Task CreateDocument(string resourceToken, dynamic doc)
 		{
			// Get the endpoint to connect to Cosmos DB with a resource token rather than master key
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			var endpoint = config["CosmosEndpoint"];

			using (var client = new CosmosClient(endpoint, resourceToken))
			{
				var container = client.GetContainer("multi-tenant", "tenant-data");

				Console.WriteLine();
				Console.WriteLine($"Creating document with partition key {doc.tenantId}");
				try
				{
					await container.CreateItemAsync(doc, new PartitionKey(doc.tenantId));
					Console.WriteLine($"Successfully created document with partition key {doc.tenantId}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unable to create document with partition key {doc.tenantId}: {ex.Message}");
				}
			}
		}

		private async static Task QueryDocuments(string resourceToken, string pk)
		{
			// Get the endpoint to connect to Cosmos DB with a resource token rather than master key
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			var endpoint = config["CosmosEndpoint"];

			using (var client = new CosmosClient(endpoint, resourceToken))
			{
				var container = client.GetContainer("multi-tenant", "tenant-data");

				Console.WriteLine();
				Console.WriteLine($"Retrieving document");
				try
				{
					var sql = "SELECT * FROM c";
					var iterator = container.GetItemQueryIterator<dynamic>(sql, requestOptions: new QueryRequestOptions
					{
						PartitionKey = new PartitionKey(pk)
					});
					var page = await iterator.ReadNextAsync();

					Console.WriteLine($"Successfully retrieved documents with partition key {pk}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unable to retrieve documents with partition key {pk}: {ex.Message}");
				}
			}
		}

		private async static Task ReadDocument(string resourceToken, string pk, string id)
		{
			// Get the endpoint to connect to Cosmos DB with a resource token rather than master key
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			var endpoint = config["CosmosEndpoint"];

			using (var client = new CosmosClient(endpoint, resourceToken))
			{
				var container = client.GetContainer("multi-tenant", "tenant-data");

				Console.WriteLine();
				Console.WriteLine($"Retrieving document");
				try
				{
					var item = await container.ReadItemAsync<dynamic>(id, new PartitionKey(pk));

					Console.WriteLine($"Successfully retrieved document with partition key {pk}, id {id}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unable to retrieve document with partition key {pk}, id {id}: {ex.Message}");
				}
			}
		}

	}
}
