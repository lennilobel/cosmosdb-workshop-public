using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.Rag.Assistant
{
	internal class MoviesAssistantDemo
	{
		internal async Task Run()
		{
			var question = "Do you know any good sci-fi movies?";

			Console.WriteLine(question);

			var embeddingItems = await this.GetEmbeddingItemsAsync(question);
			var vectors = embeddingItems[0].Embedding.ToArray();

			foreach (var vector in vectors)
			{
				Console.WriteLine(vector);
			}
		}

		private async Task<IReadOnlyList<EmbeddingItem>> GetEmbeddingItemsAsync(string input)
		{
			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: Shared.AppConfig.OpenAI.DeploymentName,
				input: [input]);

			var embeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
			var embeddingItems = embeddings.Value.Data;

			return embeddingItems;
		}

	}
}
