using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.Rag.Demos
{
	public static class MoviesAssistantDemo
	{
		private static string[] Questions = [
			"Please recommend some good sci-fi movies",
			"Do you know any good mobster movies?",
			"Do you know any movies produced by Pixar?",
			"Can you recommend movies in Italian?",
			"Actually, I meant just comedies",
			"Can you recommend movies made before the year 2000?",
			"Can you recommend movies made after the year 2000?",
			"I love horror flicks",
		];

		public static async Task Run()
		{
			Debugger.Break();

			var completionsOptions = InitializeCompletionOptions();

			foreach (var question in Questions)
			{
				await ProcessQuestion(question, completionsOptions);
			}
		}

		private static ChatCompletionsOptions InitializeCompletionOptions()
		{
			var completionsOptions = new ChatCompletionsOptions
			{
				MaxTokens = 400,
				Temperature = 1f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				NucleusSamplingFactor = 0.95f, // Top P
				DeploymentName = Shared.AppConfig.OpenAI.CompletionsDeploymentName,
			};

			var content = @"
You are a movies enthusiast who helps people discover films that they would enjoy watching.
You are upbeat and friendly. Always include the language and year of each movie recommendation.
			";

			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(content);
			Console.ForegroundColor = ConsoleColor.Gray;

			completionsOptions.Messages.Add(new ChatRequestSystemMessage(content));

			return completionsOptions;
		}

		private static async Task ProcessQuestion(string question, ChatCompletionsOptions completionsOptions)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(question);
			Console.ResetColor();

			var queryEmbeddings = await GenerateQueryEmbeddings(question);
			var vectors = queryEmbeddings[0].Embedding.ToArray();
			var results = await RunVectorSearch(vectors);

			var counter = 0;
			foreach (var result in results)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"{++counter}. {result.title}");
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(JsonConvert.SerializeObject(result));
				Console.ResetColor();
			}

			var answer = await GenerateAnswer(question, results, completionsOptions);

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(answer);
			Console.ResetColor();
		}

		private static async Task<IReadOnlyList<EmbeddingItem>> GenerateQueryEmbeddings(string question)
		{
			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: Shared.AppConfig.OpenAI.EmbeddingsDeploymentName,
				input: [question]);

			var embeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
			var embeddingItems = embeddings.Value.Data;

			return embeddingItems;
		}

		private static async Task<dynamic[]> RunVectorSearch(float[] vectors)
		{
			var database = Shared.CosmosClient.GetDatabase(Shared.AppConfig.CosmosDb.DatabaseName);
			var container = database.GetContainer(Shared.AppConfig.CosmosDb.ContainerName);

			var sql = @"
				SELECT TOP 5
					vd.id,
					vd.title,
					vd.budget,
					vd.genres,
					vd.original_language,
					vd.original_title,
					vd.overview,
					vd.popularity,
					vd.production_companies,
					vd.release_date,
					vd.revenue,
					vd.runtime,
					vd.spoken_languages,
					vd.video,
					vd.similarity_score
				FROM (
					SELECT
						c.id,
						c.title,
						c.budget,
						c.genres,
						c.original_language,
						c.original_title,
						c.overview,
						c.popularity,
						c.production_companies,
						c.release_date,
						c.revenue,
						c.runtime,
						c.spoken_languages,
						c.video,
						VectorDistance(c.vectors, @vectors, false) AS similarity_score
					FROM
						c
				) AS vd
				WHERE
					vd.similarity_score > 0
				ORDER BY
					vd.similarity_score DESC
			";

			var query = new QueryDefinition(sql)
				.WithParameter("@vectors", vectors);

			var iterator = container.GetItemQueryIterator<dynamic>(query);

			var results = new List<dynamic>();
			while (iterator.HasMoreResults)
			{
				var page = await iterator.ReadNextAsync();
				foreach (var result in page)
				{
					results.Add(result);
				}
			}
			
			return results.ToArray();
		}

		private static async Task<string> GenerateAnswer(string question, dynamic[] results, ChatCompletionsOptions completionsOptions)
		{
			var prompts = new StringBuilder();
			prompts.AppendLine($"After asking the question '{question}', the database returned the following movie recommendations:");

			var counter = 0;
			foreach (var result in results)
			{
				prompts.AppendLine($"{++counter}. Title: {result.title}");
				prompts.AppendLine($"   Overview: {result.overview}");
				prompts.AppendLine($"   Similarity Score: {result.similarity_score:F2}");
				prompts.AppendLine();

				counter++;
			}

			prompts.AppendLine($"Generate a natural language response of the recommendations.");

			var content = prompts.ToString();

			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(content);
			Console.ForegroundColor = ConsoleColor.Gray;

			completionsOptions.Messages.Add(new ChatRequestUserMessage(content));

			var completions = await Shared.OpenAIClient.GetChatCompletionsAsync(completionsOptions);
			var answer = completions.Value.Choices[0].Message.Content;

			return answer;
		}

	}
}
