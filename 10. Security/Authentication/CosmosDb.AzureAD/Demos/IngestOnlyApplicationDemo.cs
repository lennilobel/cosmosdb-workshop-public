using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace CosmosDb.AccessControl.Demos
{
	public static class IngestOnlyApplicationDemo
	{
		public async static Task Run()
		{
			Debugger.Break();

			Console.WriteLine();
			Console.WriteLine(">>> Ingest-Only Application <<<");
			Console.WriteLine();

			// Get access to the configuration file
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			// Get the Cosmos DB account endpoint
			var endpoint = config["CosmosEndpoint"];

			// Get AAD directory ID, plus the client ID and secret for the ingest-only application
			var directoryId = config["AadDirectoryId"];
			var clientId = config["AadIngestOnlyClientId"];
			var clientSecret = config["AadIngestOnlyClientSecret"];

			// Create a Cosmos client from the AAD directory ID with the client ID and client secret
			var credential = new ClientSecretCredential(directoryId, clientId, clientSecret);
			using var client = new CosmosClient(endpoint, credential);

			// Get a reference to the container
			var container = client.GetContainer("iot-demo", "iot");

			// Succeeds because role definition allows creating documents
			var doc = new { id = "Event1", deviceId = "Device1", temperature = 120 };
			await container.CreateItemAsync(doc, new PartitionKey("Device1"));
			Console.WriteLine("Successfully created new document");

			// Fails because role definition does not allow point reads
			try
			{
				await container.ReadItemAsync<dynamic>("Event1", new PartitionKey("Device1"));
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Forbidden) // 403
			{
				Console.WriteLine("Permission denied for point read");
			}

			// Fails because role definition does not allow queries
			try
			{
				await container.GetItemQueryIterator<dynamic>("SELECT * FROM c").ReadNextAsync();
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Forbidden) // 403
			{
				Console.WriteLine("Permission denied for query");
			}
		}

	}
}
