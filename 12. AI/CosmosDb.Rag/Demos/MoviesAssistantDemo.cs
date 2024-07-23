using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDb.Rag.Demos
{
	public static class MoviesAssistantDemo
	{
		// UX behavior
		private static readonly bool _verbose = false;			// Display background operations (completion messages, vector search)
		private static readonly bool _streamOutput = true;		// Stream output to simulate reading and writing
		private static readonly bool _interactive = true;		// Wait for the user to press Enter for each question

		// AI behavior
		private static readonly bool _includeLanguageAndYear = false;	// Include the movie language and year in each response
		private static readonly bool _noEmojis = false;					// Don't include emojies in the response
		private static readonly bool _noMarkdown = false;				// Don't format markdown in the response
		private static readonly bool _generatePosterImage = false;		// Generate a movie poster based on the response (DALL-E)
		private static readonly string _responseLanguage = "English";	// Translate the natural language response to any other language

		// Timings
		private static TimeSpan _elapsedVectorizeQuestion;
		private static TimeSpan _elapsedRunVectorSearch;
		private static TimeSpan _elapsedGenerateAnswer;
		private static TimeSpan _elapsedGeneratePoster;

		// List your natural language movie questions here...
		private static string[] Questions = [
			"Please recommend some good sci-fi movies",
			"Actually, I meant action/adventure sci-fi",
			"Got any Star Wars movies?",
			"Do you know any good mobster movies?",
			"Do you know any movies produced by Pixar?",
			"Can you recommend movies in Italian?",
			"Actually, I meant just comedies in that language",
			"Can you recommend movies made before the year 2000?",
			"I love horror flicks",
		];

		public static async Task RunMoviesAssistant()
		{
			Debugger.Break();

			SayHello();

			var completionsOptions = InitializeCompletionOptions();

			SetChatPrompt(completionsOptions);

			foreach (var question in Questions)
			{
				if (!await ProcessQuestion(question, completionsOptions))
				{
					break;
				}
			}
		}

		private static void SayHello()
		{
			Console.OutputEncoding = Encoding.UTF8;
			Console.Clear();

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(@"___  ___           _              ___          _     _              _   ");
			Console.WriteLine(@"|  \/  |          (_)            / _ \        (_)   | |            | |  ");
			Console.WriteLine(@"| .  . | _____   ___  ___  ___  / /_\ \___ ___ _ ___| |_ __ _ _ __ | |_ ");
			Console.WriteLine(@"| |\/| |/ _ \ \ / / |/ _ \/ __| |  _  / __/ __| / __| __/ _` | '_ \| __|");
			Console.WriteLine(@"| |  | | (_) \ V /| |  __/\__ \ | | | \__ \__ \ \__ \ || (_| | | | | |_ ");
			Console.WriteLine(@"\_|  |_/\___/ \_/ |_|\___||___/ \_| |_/___/___/_|___/\__\__,_|_| |_|\__|");
			Console.ResetColor();
		}

		private static ChatCompletionsOptions InitializeCompletionOptions() =>
			new()
			{
				MaxTokens = 1000,					// The more tokens you specify (spend), the more verbose the response
				Temperature = 1.0f,					// Range is 0.0 to 2.0; controls "apparent creativity"; higher = more random, lower = more deterministic
				FrequencyPenalty = 0.0f,			// Range is -2.0 to 2.0; controls likelihood of repeating words; higher = less likely, lower = more likely
				PresencePenalty = 0.0f,				// Range is -2.0 to 2.0; controls likelihood of introducing new topics; higher = more likely, lower = less likely
				NucleusSamplingFactor = 0.95f,		// Range is 0.0 to 2.0; aka "Top P sampling"; temperature alternative
				DeploymentName =                    // GPT model
					Shared.AppConfig.OpenAI.CompletionsDeploymentName,	
			};

		private static void SetChatPrompt(ChatCompletionsOptions completionsOptions)
		{
			var sb = new StringBuilder();
			sb.Append("You are a movies enthusiast who helps people discover films that they would enjoy watching. ");
			sb.Append("You are upbeat and friendly. ");
			sb.Append("Always include the genres of each movie recommendation. ");

			if (_responseLanguage != "English")
			{
				sb.Append($"Always translate your recommendations in {_responseLanguage}. ");
			}

			if (_includeLanguageAndYear)
			{
				sb.Append("Always include the language and year of each movie recommendation. ");
			}

			if (_noEmojis)
			{
				sb.Append("Don't include emojis, because they won't render in my demo console application. ");
			}

			if (_noMarkdown)
			{
				sb.Append("Don't include markdown syntax, because it won't render in my demo console application. ");
			}

			var prompt = sb.ToString();

			DisplayPromptMessage(prompt);

			completionsOptions.Messages.Add(new ChatRequestSystemMessage(prompt));
		}

		private static async Task<bool> ProcessQuestion(string question, ChatCompletionsOptions completionsOptions)
		{
			if (!DisplayUserQuestion(question))
			{
				return false;
			}

			// Step 1 - Generate vectors from a natural language query (Embeddings API using a text embedding model)
			var vectors = await VectorizeQuestion(question);

			// Step 2 - Run a vector search in our database (Cosmos DB NoSQL API vector support)
			var results = await RunVectorSearch(vectors);

			// Step 3 - Generate a natural language response (Completions API using a GPT model)
			var answer = await GenerateAnswer(question, results, completionsOptions);

			DisplayAssistantResponse(answer);

			if (_generatePosterImage)
			{
				// Step 4 - Generate an image based on the results (DALL-E model)
				await GeneratePosterImage(results);
			}

			DisplayElapsedTimes();

			return true;
		}

		private static async Task<float[]> VectorizeQuestion(string question)
		{
			var started = DateTime.Now;

			DisplayWaitingFor("Vectorizing question");

			var embeddingsOptions = new EmbeddingsOptions(
				deploymentName: Shared.AppConfig.OpenAI.EmbeddingsDeploymentName,	// Text embeddings model
				input: new[] { question });											// Natural language query

			var embeddings = await Shared.OpenAIClient.GetEmbeddingsAsync(embeddingsOptions);
			var embeddingItems = embeddings.Value.Data;
			var vectors = embeddingItems[0].Embedding.ToArray();

			_elapsedVectorizeQuestion = DateTime.Now.Subtract(started);

			return vectors;
		}

		private static async Task<dynamic[]> RunVectorSearch(float[] vectors)
		{
			var started = DateTime.Now;

			DisplayWaitingFor("Running vector search");

			var database = Shared.CosmosClient.GetDatabase(Shared.AppConfig.CosmosDb.DatabaseName);
			var container = database.GetContainer(Shared.AppConfig.CosmosDb.ContainerName);

			// Use the VectorDistance function to calculate a similarity score, and use TOP n with ORDER BY to retrieve the most relevant documents
			//  (by using a subquery, we only need to call VectorDistance once in the inner SELECT clause, and can reuse it in the outer ORDER BY clause)
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
                ORDER BY
                    vd.similarity_score DESC
            ";

			if (_verbose)
			{
				DisplayHeading("COSMOS VECTOR SEARCH QUERY", ConsoleColor.Green);
				DisplayMessage(sql, ConsoleColor.Green);
			}

			var query = new QueryDefinition(sql).WithParameter("@vectors", vectors);
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

			if (_verbose)
			{
				DisplayHeading("COSMOS VECTOR SEARCH RESULT", ConsoleColor.Green);

				var counter = 0;
				foreach (var result in results)
				{
					DisplayMessage($"{++counter}. {result.title}", ConsoleColor.Green);
					DisplayMessage(JsonConvert.SerializeObject(result));
				}
			}

			_elapsedRunVectorSearch = DateTime.Now.Subtract(started);

			return results.ToArray();
		}

		private static async Task<string> GenerateAnswer(string question, dynamic[] results, ChatCompletionsOptions completionsOptions)
		{
			var started = DateTime.Now;

			DisplayWaitingFor("Generating response");

			var sb = new StringBuilder();
			sb.AppendLine($"After asking the question '{question}', the database returned the following movie recommendations:");

			var counter = 0;
			foreach (var result in results)
			{
				sb.AppendLine($"{++counter}. Title: {result.title}");
				sb.AppendLine($"   Overview: {result.overview}");
				sb.AppendLine($"   Similarity Score: {result.similarity_score:F2}");
				sb.AppendLine();
			}

			sb.AppendLine($"Generate a natural language response of the recommendations.");

			var userMessagePrompt = sb.ToString();

			DisplayPromptMessage(userMessagePrompt);

			completionsOptions.Messages.Add(new ChatRequestUserMessage(userMessagePrompt));

			var completions = await Shared.OpenAIClient.GetChatCompletionsAsync(completionsOptions);
			var answer = completions.Value.Choices[0].Message.Content;

			_elapsedGenerateAnswer = DateTime.Now.Subtract(started);

			return answer;
		}

		private static async Task GeneratePosterImage(dynamic[] results)
		{
			var started = DateTime.Now;

			var sb = new StringBuilder();
			sb.AppendLine("I am planning a 'Movie Discussion Night' event, where we will get together and discuss each of these movies:");

			var counter = 0;
			foreach (var result in results)
			{
				sb.AppendLine($"{++counter}. {result.title}");
			}

			sb.AppendLine("Make a collage poster depicting one image based on each movie.");
			sb.AppendLine("Generate a title \"Movie Discussion Night\" in big letters at the top of the poster.");
			sb.AppendLine("Generate a subtitle that says \"Let's Discuss...\".");
			
			var imagePrompt = sb.ToString();

			DisplayPromptMessage(imagePrompt);

			var response = default(Azure.Response<ImageGenerations>);
			try
			{
				response = await Shared.OpenAIClient.GetImageGenerationsAsync(
					new ImageGenerationOptions()
					{
						DeploymentName = Shared.AppConfig.OpenAI.DalleDeploymentName,
						Prompt = imagePrompt,
						Size = ImageSize.Size1024x1792,
						Quality = ImageGenerationQuality.Standard,
					});
			}
			catch (Exception ex)
			{
				DisplayMessage("Error generating poster image", ConsoleColor.Red);
				DisplayMessage(ex.Message, ConsoleColor.Red);
			}

			_elapsedGeneratePoster = DateTime.Now.Subtract(started);

			if (response == null)
			{
				return;
			}

			var generatedImage = response.Value.Data[0];
			if (!string.IsNullOrEmpty(generatedImage.RevisedPrompt) && _verbose)
			{
				DisplayPromptMessage($"Input prompt revised to:\n{generatedImage.RevisedPrompt}");
			}

			DisplayAssistantResponse($"Generated image is ready at:\n{generatedImage.Url.AbsoluteUri}");
			OpenBrowser(generatedImage.Url.AbsoluteUri);
		}

		#region Helpers

		private static void OpenBrowser(string url)
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true
				};
				Process.Start(psi);
			}
			catch (Exception ex)
			{
				DisplayMessage($"Error opening browser: {ex.Message}", ConsoleColor.Red);
			}
		}

		private static void DisplayElapsedTimes()
		{
			Console.WriteLine();
			DisplayMessage($"Vectorized question:     {_elapsedVectorizeQuestion}");
			DisplayMessage($"Ran vector search:       {_elapsedRunVectorSearch}");
			DisplayMessage($"Generated response:      {_elapsedGenerateAnswer}");
			if (_generatePosterImage)
			{
				DisplayMessage($"Generated poster image:  {_elapsedGeneratePoster}");
			}
		}

		private static void DisplayPromptMessage(string text)
		{
			if (!_verbose)
			{
				return;
			}

			DisplayHeading("PROMPT MESSAGE", ConsoleColor.Magenta);
			DisplayMessage(text, ConsoleColor.Magenta);
		}

		private static bool DisplayUserQuestion(string text)
		{
			DisplayHeading("USER QUESTION", ConsoleColor.Yellow);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("> ");
			if (_interactive && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
			{
				Console.ResetColor();
				return false;
			}
			DisplayMessage(text, ConsoleColor.Yellow, streamChunkSize: 1, suppressLineFeed: true);
			if (_interactive && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
			{
				Console.ResetColor();
				return false;
			}
			Console.Write(Environment.NewLine);

			return true;
		}

		private static void DisplayAssistantResponse(string text)
		{
			if (!_verbose)
			{
				Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
				Console.Write(new string(' ', Console.WindowWidth));
			}

			DisplayHeading("ASSISTANT RESPONSE", ConsoleColor.Cyan);
			DisplayMessage(text, ConsoleColor.Cyan, streamChunkSize: 10);
		}

		private static void DisplayWaitingFor(string text)
		{
			if (_verbose)
			{
				return;
			}

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write($"   {text}...");
			Console.ResetColor();
		}

		private static void DisplayHeading(string text, ConsoleColor backColor)
		{
			var foreColor =
					backColor == ConsoleColor.Yellow ||
					backColor == ConsoleColor.Cyan ||
					backColor == ConsoleColor.Green
						? ConsoleColor.Black
						: ConsoleColor.White;

			Console.WriteLine();
			Console.BackgroundColor = backColor;
			Console.ForegroundColor = foreColor;
			Console.Write($"=== {text} ===");
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine();
		}

		private static void DisplayMessage(string text, ConsoleColor color = ConsoleColor.Gray, int? streamChunkSize = null, bool suppressLineFeed = false)
		{
			Console.ForegroundColor = color;

			if (streamChunkSize != null && _streamOutput)
			{
				for (var i = 0; i < text.Length; i += streamChunkSize.Value)
				{
					var chunk = text.Substring(i, Math.Min(streamChunkSize.Value, text.Length - i));
					Console.Write(chunk);
					Thread.Sleep(1);
				}
			}
			else
			{
				Console.Write(text);
			}

			if (!suppressLineFeed)
			{
				Console.Write(Environment.NewLine);
			}

			Console.ResetColor();
		}

		#endregion

	}
}
