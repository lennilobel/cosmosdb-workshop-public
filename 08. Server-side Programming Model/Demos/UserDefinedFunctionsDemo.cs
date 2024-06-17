using Microsoft.Azure.Cosmos.Scripts;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.ServerSide.Demos
{
	public static class UserDefinedFunctionsDemo
	{
		public static async Task Run()
		{
			Debugger.Break();

			await CreateUserDefinedFunctions();

			await ViewUserDefinedFunctions();

			await Execute_udfCalculatePremium();
			await Execute_udfIsNorthAmerica();
			await Execute_udfFormatCityStateZip();

			await DeleteUserDefinedFunctions();
		}

		private static async Task CreateUserDefinedFunctions()
		{
			Console.Clear();
			Console.WriteLine(">>> Create User Defined Functions <<<");
			Console.WriteLine();

			await CreateUserDefinedFunction("udfCalculatePremium");
			await CreateUserDefinedFunction("udfIsNorthAmerica");
			await CreateUserDefinedFunction("udfFormatCityStateZip");
		}

		private static async Task CreateUserDefinedFunction(string udfId)
		{
			var udfBody = File.ReadAllText($@"Server\{udfId}.js");
			var udfProps = new UserDefinedFunctionProperties
			{
				Id = udfId,
				Body = udfBody
			};

			var container = Shared.Client.GetContainer("adventure-works", "stores");
			var result = await container.Scripts.CreateUserDefinedFunctionAsync(udfProps);
			Console.WriteLine($"Created user defined function  {udfId} ({result.RequestCharge} RUs);");
		}

		private static async Task ViewUserDefinedFunctions()
		{
			Console.Clear();
			Console.WriteLine(">>> View UDFs <<<");
			Console.WriteLine();

			var container = Shared.Client.GetContainer("adventure-works", "stores");

			var iterator = container.Scripts.GetUserDefinedFunctionQueryIterator<UserDefinedFunctionProperties>();
			var udfs = await iterator.ReadNextAsync();

			var count = 0;
			foreach (var udf in udfs)
			{
				count++;
				Console.WriteLine($" UDF Id: {udf.Id};");
			}

			Console.WriteLine();
			Console.WriteLine($"Total UDFs: {count}");
		}

		private static async Task Execute_udfCalculatePremium()
		{
			Console.Clear();
			Console.WriteLine("Querying with calculated premiums");
			Console.WriteLine();

			var sql = @"
				SELECT
					c.id,
					c.name,
					c.address.countryRegionName AS country,
					c.address.location.stateProvinceName AS state,
					100 AS amount,
					udf.udfCalculatePremium(100, c.address.countryRegionName, c.address.location.stateProvinceName) AS amountPlusPremium
				FROM
					c
				ORDER BY
					c.address.countryRegionName
			";

			var container = Shared.Client.GetContainer("adventure-works", "stores");
			var documents = (await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync()).ToList();

			Console.WriteLine($"Found {documents.Count} customers:");
			foreach (var document in documents)
			{
				Console.WriteLine($"{document.id, 5} {document.name, -42} {document.country, -20} {document.state, -20} {document.amount} {document.amountPlusPremium}");
			}
		}

		private static async Task Execute_udfIsNorthAmerica()
		{
			Console.Clear();
			Console.WriteLine("Querying for North American customers");

			var sql = @"
				SELECT c.name, c.address.countryRegionName
				FROM c
				WHERE udf.udfIsNorthAmerica(c.address.countryRegionName) = true
				ORDER BY c.name";

			var container = Shared.Client.GetContainer("adventure-works", "stores");
			var documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();

			Console.WriteLine($"Found {documents.Count} North American customers:");
			foreach (var document in documents)
			{
				Console.WriteLine($" {document.name, -42} {document.countryRegionName}");
			}

			sql = @"
				SELECT c.name, c.address.countryRegionName
				FROM c
				WHERE udf.udfIsNorthAmerica(c.address.countryRegionName) = false
				ORDER BY c.name";

			Console.WriteLine();
			Console.WriteLine("Querying for non North American customers");

			documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();

			Console.WriteLine($"Found {documents.Count} non North American customers:");
			foreach (var document in documents)
			{
				Console.WriteLine($" {document.name, -42} {document.countryRegionName}");
			}
		}

		private static async Task Execute_udfFormatCityStateZip()
		{
			Console.Clear();
			Console.WriteLine("Listing names with formatted city, state, zip");

			var sql = "SELECT c.name, udf.udfFormatCityStateZip(c) AS csz FROM c ORDER BY c.name";

			var container = Shared.Client.GetContainer("adventure-works", "stores");
			var documents = (await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync()).ToList();
			foreach (var document in documents)
			{
				Console.WriteLine($" {document.name, -42} {document.csz}");
			}
		}

		private static async Task DeleteUserDefinedFunctions()
		{
			Console.WriteLine();
			Console.WriteLine(">>> Delete User Defined Functions <<<");
			Console.WriteLine();

			await DeleteUserDefinedFunction("udfCalculatePremium");
			await DeleteUserDefinedFunction("udfIsNorthAmerica");
			await DeleteUserDefinedFunction("udfFormatCityStateZip");
		}

		private static async Task DeleteUserDefinedFunction(string udfId)
		{
			var container = Shared.Client.GetContainer("adventure-works", "stores");
			await container.Scripts.DeleteUserDefinedFunctionAsync(udfId);

			Console.WriteLine($"Deleted UDF: {udfId}");
		}

	}
}
