using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace CosmosDb.Rag.Demos
{
	public static class VectorEmbeddingsDemo
	{
		private static class Context
		{
			public static int ItemCount;
			public static int ErrorCount;
			public static double RuCost;
		}

		public static async Task Run()
		{
			Debugger.Break();

			var started = DateTime.Now;

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine();
			Console.WriteLine($"Generating and embedding vectors");

			Context.ItemCount = 0;
			Context.ErrorCount = 0;
			Context.RuCost = 0;

			var database = Shared.CosmosClient.GetDatabase(Shared.AppConfig.CosmosDb.DatabaseName);
			var container = database.GetContainer(Shared.AppConfig.CosmosDb.ContainerName);
			await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 10000));

			var iterator = container.GetItemQueryIterator<JObject>(
				queryText: "SELECT * FROM c",
				requestOptions: new QueryRequestOptions { MaxItemCount = 100 });

			while (iterator.HasMoreResults)
			{
				var batchStarted = DateTime.Now;

				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write($"Retrieving documents... ");
				var documents = (await iterator.ReadNextAsync()).ToArray();
				Console.WriteLine(documents.Length);

				Context.ItemCount += documents.Length;

				var embeddings = await GenerateEmbeddings(documents);
				await UpdateDocuments(container, documents, embeddings);

				var batchElapsed = DateTime.Now.Subtract(batchStarted);

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"Processed documents {Context.ItemCount - documents.Length + 1} - {Context.ItemCount} in {batchElapsed}");
			}

			await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(autoscaleMaxThroughput: 1000));
			var elapsed = DateTime.Now.Subtract(started);

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"Generated and embedded vectors for {Context.ItemCount} document(s) with {Context.ErrorCount} error(s) in {elapsed} ({Context.RuCost} RUs)");
			Console.ResetColor();
		}

		private static async Task<IReadOnlyList<EmbeddingItem>> GenerateEmbeddings(JObject[] documents)
		{
			Console.Write($"Generating embeddings... ");

			for (var i = 0; i < documents.Length; i++)
			{
				// Discard system properties
				documents[i].Remove("vectors");
			}

			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: Shared.AppConfig.OpenAI.EmbeddingsDeploymentName,
				input: documents.Select(d => d.ToString()));

			var openAIEmbeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
			var embeddings = openAIEmbeddings.Value.Data;

			Console.WriteLine(embeddings.Count);

			return embeddings;
		}

		private static async Task UpdateDocuments(Container container, JObject[] documents, IReadOnlyList<EmbeddingItem> embeddings)
		{
			Console.Write($"Updating documents... ");

			for (var i = 0; i < documents.Length; i++)
			{
				var embeddingsArray = embeddings[i].Embedding.ToArray();
				var vectors = JArray.FromObject(embeddingsArray);
				documents[i]["vectors"] = vectors;
			}

			var tasks = new List<Task>(documents.Length);
			foreach (JObject document in documents)
			{
				var task = container.ReplaceItemAsync(document, document["id"].ToString(), new PartitionKey("movie"));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status == TaskStatus.RanToCompletion)
						{
							Context.RuCost += t.Result.RequestCharge;
						}
						else
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine($"Error replacing document id='{document["id"]}', title='{document["title"]}'\n{t.Exception.Message}");
							Console.ForegroundColor = ConsoleColor.Green;
							Context.ErrorCount++;
						}
					}));
			}

			await Task.WhenAll(tasks);

			Console.WriteLine(documents.Length);
		}

	}
}
