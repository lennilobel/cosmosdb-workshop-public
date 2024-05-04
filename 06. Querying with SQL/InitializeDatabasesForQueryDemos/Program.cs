using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace InitializeDatabasesForQueryDemos
{
	class Program
	{
		static IConfigurationRoot _config;

		static async Task Main(string[] args)
		{
			_config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

			Console.WriteLine("Initialize databases for Cosmos DB SQL query  demos");
			while (true)
			{
				Console.WriteLine();
				Console.Write("F=Families / AJ=AdventureWorks (JSON) / AS=AdventureWorks (SQL) / E=Exit: ");
				var input = Console.ReadLine().Trim().ToUpper();
				if (input == "F") await CreateFamiliesDatabaseForQueryDemos();
				if (input == "AJ") await CreateAdventureWorksDatabaseFromJson();
				if (input == "AS") await CreateAdventureWorksDatabaseFromSqlServer();
				if (input == "E") break;
			}
		}

		private static async Task CreateFamiliesDatabaseForQueryDemos()
		{
			var started = DateTime.Now;

			var andersen = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(@".\FamilyAndersen.json"));
			var smith = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(@".\FamilySmith.json"));
			var wakefield = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(@".\FamilyWakefield.json"));

			using var client = GetClient();

			var database = await CreateDatabaseWithOverwrite(client, "Families");
			var container = (await database.CreateContainerAsync("Families", "/location/state", 400)).Container;

			await container.CreateItemAsync(andersen, new PartitionKey(andersen["location"]["state"].Value<string>()));
			await container.CreateItemAsync(smith, new PartitionKey(smith["location"]["state"].Value<string>()));
			await container.CreateItemAsync(wakefield, new PartitionKey(wakefield["location"]["state"].Value<string>()));

			var elapsed = DateTime.Now.Subtract(started);

			Console.WriteLine($"Successfully created Families database Families container in {elapsed}");
		}

		private static async Task CreateAdventureWorksDatabaseFromJson()
		{
			var started = DateTime.Now;
			var json = File.ReadAllText(@".\AdventureWorks.json");
			var list = JsonConvert.DeserializeObject<List<JObject>>(json);

			using var client = GetClient();

			var database = await CreateDatabaseWithOverwrite(client, "adventure-works");
			var container = (await database.CreateContainerAsync("stores", "/address/postalCode", 4000)).Container;

			var tasks = new List<Task>(list.Count);
			foreach (var documentDef in list)
			{
				var task = container.CreateItemAsync(documentDef, new PartitionKey(documentDef["address"]["postalCode"].ToString()));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status != TaskStatus.RanToCompletion)
						{
							Console.WriteLine($"Error creating document: {t.Exception.Message}");
						}
					}));
			}
			await Task.WhenAll(tasks);
			
			await container.ReplaceThroughputAsync(ThroughputProperties.CreateManualThroughput(400));

			var elapsed = DateTime.Now.Subtract(started);

			Console.WriteLine($"Successfully created adventure-works database stores container from raw JSON in {elapsed}");
		}

		private static async Task CreateAdventureWorksDatabaseFromSqlServer()
		{
			var started = DateTime.Now;

			using var conn = new SqlConnection(_config["SqlDatabaseConnectionString"]);

			conn.Open();
			using var cmd = conn.CreateCommand();

			cmd.CommandText = File.ReadAllText(@".\AdventureWorks.sql");
			using var rdr = cmd.ExecuteReader();

			var list = new List<(object DocumentDef, string PostalCode)>();

			while (rdr.Read())
			{
				dynamic documentDef = new
				{
					id = rdr["id"].ToString(),      // If you omit .ToString() here, then id is of type "object" instead of string, and the operation fails with a 400, but without a proper reason
					name = rdr["name"],
					address = new
					{
						addressType = rdr["address.addressType"],
						addressLine1 = rdr["address.addressLine1"],
						location = new
						{
							city = rdr["address.location.city"],
							stateProvinceName = rdr["address.location.stateProvinceName"],
						},
						postalCode = rdr["address.postalCode"],
						countryRegionName = rdr["address.countryRegionName"]
					}
				};
				list.Add((documentDef, documentDef.address.postalCode));
			}

			conn.Close();

			using var client = GetClient();

			var database = await CreateDatabaseWithOverwrite(client, "adventure-works");
			var container = (await database.CreateContainerAsync("stores", "/address/postalCode", 2000)).Container;

			var tasks = new List<Task>(list.Count);
			foreach (var (documentDef, postalCode) in list)
			{
				var task = container.CreateItemAsync(documentDef, new PartitionKey(postalCode));
				tasks.Add(task
					.ContinueWith(t =>
					{
						if (t.Status != TaskStatus.RanToCompletion)
						{
							Console.WriteLine($"Error creating document: {t.Exception.Message}");
						}
					}));
			}
			await Task.WhenAll(tasks);

			await container.ReplaceThroughputAsync(ThroughputProperties.CreateManualThroughput(400));

			var elapsed = DateTime.Now.Subtract(started);

			Console.WriteLine($"Successfully created adventure-works database stores container from SQL database in {elapsed}");
		}

		private static async Task<Database> CreateDatabaseWithOverwrite(CosmosClient client, string databaseId)
		{
			try
			{
				await client.GetDatabase(databaseId).DeleteAsync();
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
			}

			var database = (await client.CreateDatabaseAsync(databaseId)).Database;

			return database;
		}

		private static CosmosClient GetClient()
		{
			var endpoint = _config["CosmosEndpoint"];
			var masterKey = _config["CosmosMasterKey"];

			var client = new CosmosClient(endpoint, masterKey, new CosmosClientOptions { AllowBulkExecution = true });

			return client;
		}
	}
}
