using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CosmosDb.Rag.Embed.Function.V4
{
	public class VectorEmbeddingFunction
	{
		private const string DatabaseName = "rag-demo";
		private const string ContainerName = "movies";

		private static readonly List<string> _processedIds = [];
		private static readonly object _threadLock = new();

		private readonly ILogger _logger;

		private static CosmosClient CosmosClient { get; }

		private static OpenAIClient OpenAIClient { get; }

		static VectorEmbeddingFunction()
		{
			var cosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
			CosmosClient = new CosmosClient(cosmosDbConnectionString, new CosmosClientOptions { AllowBulkExecution = true });

			var openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
			var openAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
			OpenAIClient = new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
		}

		public VectorEmbeddingFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<VectorEmbeddingFunction>();
		}

		[Function("EmbedVectors")]
        public async Task EmbedVectors(
            [CosmosDBTrigger(
				databaseName: DatabaseName,
				containerName: ContainerName,
			    Connection = "CosmosDbConnectionString",
                LeaseContainerName = "lease",
                CreateLeaseContainerIfNotExists = true
            )]
            IReadOnlyList<JsonElement> documentElements)
		{
			if (documentElements == null || documentElements.Count == 0)
			{
				return;
			}

			try
			{
				await this.ProcessChanges(documentElements);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred processing changed movie documents");
			}
		}

		private async Task ProcessChanges(IReadOnlyList<JsonElement> documentElements)
		{
			var documents = this.GetChangedDocuments(documentElements);

			if (documents.Length == 0)
			{
				return;
			}

			_logger.LogInformation($"Change detected in {documents.Length} movie document(s)");

			var embeddings = await this.GenerateVectorEmbeddings(documents);

			await this.UpdateDocuments(documents, embeddings);
		}

		private JObject[] GetChangedDocuments(IReadOnlyList<JsonElement> documentElements)
		{
			var changedDocuments = new List<JObject>();
			foreach (var documentElement in documentElements)
			{
				var document = JsonConvert.DeserializeObject<JObject>(documentElement.GetRawText());
				var id = document["id"].ToString();

				lock (_threadLock)
				{
					if (!_processedIds.Contains(id))
					{
						_logger.LogInformation($"Change detected in movie document ID {id} ({document["title"]})");
						_processedIds.Add(id);
						changedDocuments.Add(document);
					}
					else
					{
						_processedIds.Remove(id);
					}
				}
			}

			return changedDocuments.ToArray();
		}

		private async Task<IReadOnlyList<EmbeddingItem>> GenerateVectorEmbeddings(JObject[] documents)
		{
			this._logger.LogInformation($"Generating vector embeddings for {documents.Length} document(s)");

			for (var i = 0; i < documents.Length; i++)
			{
				documents[i].Remove("vectors");
			}

			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: Environment.GetEnvironmentVariable("OpenAIDeploymentName"),
				input: documents.Select(JsonConvert.SerializeObject));

			var openAIEmbeddings = await OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
			var embeddings = openAIEmbeddings.Value.Data;

			this._logger.LogInformation($"Generated vector embeddings for {documents.Length} document(s)");

			return embeddings;
		}

		private async Task UpdateDocuments(JObject[] documents, IReadOnlyList<EmbeddingItem> embeddings)
		{
			this._logger.LogInformation($"Updating {documents.Length} document(s)");

			var container = CosmosClient.GetDatabase(DatabaseName).GetContainer(ContainerName);

			for (var i = 0; i < documents.Length; i++)
			{
				var vectorsArray = embeddings[i].Embedding.ToArray();
				var vectorsJArray = JArray.FromObject(vectorsArray);
				documents[i]["vectors"] = vectorsJArray;
			}

			var tasks = new List<Task>(documents.Length);
			foreach (JObject document in documents)
			{
				var task = container.ReplaceItemAsync(document, document["id"].ToString(), new PartitionKey("movie"));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status != TaskStatus.RanToCompletion)
						{
							this._logger.LogError($"Error replacing document id='{document["id"]}', title='{document["title"]}'\n{t.Exception.Message}");
						}
					}));
			}

			await Task.WhenAll(tasks);

			this._logger.LogInformation($"Updated {documents.Length} document(s)");
		}

	}
}
