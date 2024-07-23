using CosmosDb.Rag.Demos;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CosmosDb.Rag
{
	public static class Program
	{
		private static IDictionary<string, Func<Task>> DemoMethods;

		private static void Main(string[] args)
		{
			Shared.Initialize();

			DemoMethods = new Dictionary<string, Func<Task>>
			{
				{ "PD", PopulateDatabaseDemo.CreateDatabase },
				{ "VD", VectorizeDocumentsDemo.VectorizeDocuments },
				{ "MA", MoviesAssistantDemo.RunMoviesAssistant },
				{ "UD", PopulateDatabaseDemo.UpdateDatabase },
				{ "RD", ResetDemo },
			};

			Task.Run(async () =>
			{
				ShowMenu();
				while (true)
				{
					Console.Write("Selection: ");
					var input = Console.ReadLine();
					var demoId = input.ToUpper().Trim();
					if (DemoMethods.TryGetValue(demoId, out Func<Task> value))
					{
						var demoMethod = value;
						await RunDemo(demoMethod);
					}
					else if (demoId == "Q")
					{
						break;
					}
					else
					{
						Console.WriteLine($"?{input}");
					}
				}
			}).Wait();
		}

		private static void ShowMenu()
		{
			Console.WriteLine(@$"Cosmos DB NoSQL API RAG Demos
Database: {Shared.AppConfig.CosmosDb.DatabaseName}

PD - Populate database
VD - Vectorize documents

MA - Movies assistant
UD - Update database

RD - Reset demo
Q  - Quit
");
		}

		private static async Task RunDemo(Func<Task> demoMethod)
		{
			try
			{
				await demoMethod();
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				while (ex.InnerException != null)
				{
					ex = ex.InnerException;
					message += Environment.NewLine + ex.Message;
				}
				Console.WriteLine($"Error: {ex.Message}");
			}
			Console.WriteLine();
			Console.Write("Done. Press any key to continue...");
			Console.ReadKey(true);
			Console.Clear();
			ShowMenu();
		}

		private static async Task ResetDemo()
		{
			Debugger.Break();

			var container = Shared.CosmosClient.GetContainer(Shared.AppConfig.CosmosDb.DatabaseName, Shared.AppConfig.CosmosDb.ContainerName);

			var iterator = container.GetItemQueryIterator<string>(
				"SELECT VALUE c.id FROM c WHERE c.title IN ('Star Wars', 'The Empire Strikes Back', 'Return of the Jedi')");

			var ids = (await iterator.ReadNextAsync()).ToArray();
			foreach (var id in ids)
			{
				await container.DeleteItemAsync<object>(id, new PartitionKey("movie"));
				Console.WriteLine($"Deleted movie ID {id}");
			}
		}
	}

	public static class CleanseDatasetDemo
	{
		// For the one-time cleanup of the raw movies dataset, which has some improper JSON content

		private static readonly Regex JsonStringPattern = new(@"(?<=:\s*)\""(?<value>.*?)\""", RegexOptions.Compiled);

		public static async Task Run()
		{
			Debugger.Break();

			var dirty = await File.ReadAllTextAsync("Movies_raw.json");
			var documents = JsonConvert.DeserializeObject<JArray>(dirty);
			var count = documents.Count;
			var started = DateTime.Now;
			var clean = new JArray();

			foreach (JObject document in documents)
			{
				TransformDocument(document);
				clean.Add(document);
			}

			await File.WriteAllTextAsync("..\\..\\..\\Movies.json", clean.ToString());

			Console.WriteLine($"Cleansed {count} documents in {DateTime.Now.Subtract(started)}");
		}

		private static void TransformDocument(JObject document)
		{
			// Set every movie's type to "movie" to put all the movies in the same single logical partition
			document.Add("type", "movie");

			// Remove the "adult" property
			document.Remove("adult");

			// Transform numeric properties
			document["budget"] = Convert.ToInt64(document["budget"].ToString());

			// Transform properties holding valid JSON strings into true JSON objects/arrays
			ConvertPropertyToJToken<JObject>(document, "belongs_to_collection");
			ConvertPropertyToJToken<JArray>(document, "genres");
			ConvertPropertyToJToken<JArray>(document, "production_companies");
			ConvertPropertyToJToken<JArray>(document, "production_countries");
			ConvertPropertyToJToken<JArray>(document, "spoken_languages");
		}

		private static void ConvertPropertyToJToken<T>(JObject movie, string propertyName) where T : JToken
		{
			var json =
				// Replace double-quoted JSON string values with single-quoted values, converting any single quotes within the values to double quotes.
				JsonStringPattern.Replace(movie[propertyName].ToString(), m => $"'{m.Groups["value"].Value.Replace("'", "\"")}'")
				// Fix bad JSON: None > null
				.Replace("_path': None", "_path': null");

			movie[propertyName] = JToken.Parse(json) as T;
		}

	}
}
