using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDb.Rag.Embed.Demos
{
	public static class PopulateDatabaseDemo
	{
		public static async Task Run()
		{
			Debugger.Break();

			var database = await CreateDatabase();
			var container = await CreateContainer(database);
			await CreateDocuments(container);
			await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 1000));
		}

		private static async Task<Database> CreateDatabase()
		{
			var databaseName = Shared.AppConfig.CosmosDb.DatabaseName;

			try
			{
				await Shared.CosmosClient.GetDatabase(databaseName).DeleteAsync();
				Console.WriteLine($"Deleted existing '{databaseName}' database");
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

			await Shared.CosmosClient.CreateDatabaseAsync(databaseName);
			var database = Shared.CosmosClient.GetDatabase(databaseName);

			Console.WriteLine($"Created '{databaseName}' database");

			return database;
		}

		private static async Task<Container> CreateContainer(Database database)
		{
			var containerName = Shared.AppConfig.CosmosDb.ContainerName;

			var containerProperties = new ContainerProperties
			{
				Id = containerName,
				PartitionKeyPath = "/type",
				VectorEmbeddingPolicy = new VectorEmbeddingPolicy(
					new Collection<Embedding>(
					[
						new Embedding
						{
							Path = "/vectors",
							DataType = VectorDataType.Float32,
							DistanceFunction = DistanceFunction.Cosine,
							Dimensions = 1536
						}
					])
				),
				IndexingPolicy = new IndexingPolicy
				{
					IndexingMode = IndexingMode.Consistent,
					Automatic = true,
					IncludedPaths =
					{
						new IncludedPath { Path = "/*" }
					},
					ExcludedPaths =
					{
						new ExcludedPath { Path = "/_etag/?" }
					},
					VectorIndexes =
					[
						new VectorIndexPath
						{
							Path = "/vectors",
							Type = VectorIndexType.DiskANN
						}
					]
				}
			};

			var containerThroughput = ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 10000);

			await database.CreateContainerAsync(containerProperties, containerThroughput);
			var container = database.GetContainer(containerName);

			Console.WriteLine($"Created '{containerName}' container");

			return container;
		}

		private static async Task CreateDocuments(Container container)
		{
			var json = await File.ReadAllTextAsync("Movies.json");
			var documents = JsonConvert.DeserializeObject<JArray>(json);
			var count = documents.Count;

			var cost = 0D;
			var errors = 0;
			var started = DateTime.Now;

			var tasks = new List<Task>(count);
			foreach (JObject document in documents)
			{
				var task = container.CreateItemAsync(document, new PartitionKey("movie"));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status == TaskStatus.RanToCompletion)
						{
							cost += t.Result.RequestCharge;
						}
						else
						{
							Console.WriteLine($"Error creating document id='{document["id"]}', title='{document["title"]}'\n{t.Exception.Message}");
							errors++;
						}
					}));
			}
			await Task.WhenAll(tasks);

			Console.WriteLine($"Created {count - errors} document(s) with {errors} error(s): {cost:0.##} RUs in {DateTime.Now.Subtract(started)}");
		}

	}
}
