using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebStoreDemos
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Cosmos DB WebStore Demos");
			Console.WriteLine();

			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			var endpoint = config["CosmosEndpoint"];
			var masterKey = config["CosmosMasterKey"];

			Cosmos.SetAuth(endpoint, masterKey);

			RunInteractive().Wait();
		}

		private static async Task RunInteractive()
		{
			ShowUsage();
			while (true)
			{
				Console.Write("WebStore-Demo> ");
				var input = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(input))
				{
					if ("quit".StartsWith(input.ToLower()))
					{
						break;
					}
					var args = input.Split(' ');
					await RunOperation(args);
				}
			}
		}

		private static async Task RunOperation(string[] args)
		{
			try
			{
				var operation = args[0].ToLower();
				if (operation.Matches("d1"))
				{
					await Demo1_QueryingForCustomers();
				}
				else if (operation.Matches("d2"))
				{
					await Demo2_QueryingForProductCategories();
				}
				else if (operation.Matches("d3"))
				{
					await Demo3_DenormalizationUsingTheChangeFeed();
				}
				else if (operation.Matches("d4"))
				{
					await Demo4_QueryingForSalesOrders();
				}
				else if (operation.Matches("d5"))
				{
					await Demo5_QueryingForTopCustomers();
				}
				else if (operation.Matches("reset"))
				{
					await ResetDemos();
				}
				else if (operation.Matches("help") || operation == "?")
				{
					ShowUsage();
				}
				else
				{
					throw new Exception("Unrecognized command");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				ShowUsage();
			}
		}

		private static async Task Demo1_QueryingForCustomers()
		{
			ShowDemoHeader("Demo 1 - Querying for Customers (webstore-v2 customer container)");

			var container = Cosmos.Client.GetContainer("webstore-v2", "customer");

			ShowDemoSubHeader("Get customer using SQL Query");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT * FROM c WHERE c.id = '46192BCF-E8BB-4140-A0F1-B8764A7941E7'");
			Console.ResetColor();
			PressKeyToContinue();
			var query = $"SELECT * FROM c WHERE c.id = '46192BCF-E8BB-4140-A0F1-B8764A7941E7'";
			var response1 = await container.GetItemQueryIterator<dynamic>(query).ReadNextAsync();
			var result1 = response1.FirstOrDefault();
			var cost1 = response1.RequestCharge;
			Console.WriteLine(result1);
			ShowRuCost(cost1);
			Console.WriteLine();

			ShowDemoSubHeader("Get customer using point read");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("await container.ReadItemAsync<dynamic>(");
			Console.WriteLine("  \"46192BCF-E8BB-4140-A0F1-B8764A7941E7\",");
			Console.WriteLine("  new PartitionKey(\"46192BCF-E8BB-4140-A0F1-B8764A7941E7\")");
			Console.WriteLine(");");
			Console.ResetColor();
			PressKeyToContinue();
			var response2 = await container.ReadItemAsync<dynamic>("46192BCF-E8BB-4140-A0F1-B8764A7941E7", new PartitionKey("46192BCF-E8BB-4140-A0F1-B8764A7941E7"));
			var result2 = response2.Resource;
			var cost2 = response2.RequestCharge;
			Console.WriteLine(result2);
			ShowRuCost(cost2);

			Console.WriteLine();
		}

		private static async Task Demo2_QueryingForProductCategories()
		{
			ShowDemoHeader("Demo 2 - Querying for Product Categories (webstore-v2 productCategory container)");

			var container = Cosmos.Client.GetContainer("webstore-v2", "productCategory");

			ShowDemoSubHeader("Retrieve product categories with (SELECT *)");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT * FROM c WHERE c.type = 'category'");
			Console.ResetColor();
			PressKeyToContinue();
			var query1 = $"SELECT * FROM c WHERE c.type = 'category'";
			var response1 = await container.GetItemQueryIterator<dynamic>(query1).ReadNextAsync();
			var result1 = response1.ToList();
			var cost1 = response1.RequestCharge;
			result1.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost1, result1.Count);
			Console.WriteLine();

			ShowDemoSubHeader("Retrieve product categories (SELECT id, name)");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT c.id, c.name FROM c WHERE c.type = 'category'");
			Console.ResetColor();
			PressKeyToContinue();
			var query2 = $"SELECT c.id, c.name FROM c WHERE c.type = 'category'";
			var response2 = await container.GetItemQueryIterator<dynamic>(query2).ReadNextAsync();
			var result2 = response2.ToList();
			var cost2 = response2.RequestCharge;
			result2.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost2, result2.Count);
			Console.WriteLine();
		}

		private static async Task Demo3_DenormalizationUsingTheChangeFeed()
		{
			ShowDemoHeader("Demo 3 - Denormalization using the Change Feed (webstore-v3)");

			var productCategoryContainer = Cosmos.Client.GetContainer("webstore-v3", "productCategory");
			var productContainer = Cosmos.Client.GetContainer("webstore-v3", "product");

			ShowDemoSubHeader("Retrieve the first 5 products from category 'Clothing, Shorts'");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT TOP 5 c.id, c.categoryId, c.categoryName, c.sku, c.name, c.price FROM c");
			Console.WriteLine(" WHERE c.categoryId = 'C7324EF3-D951-45D9-A345-A82EAE344394'");
			Console.ResetColor();
			PressKeyToContinue();
			var query1 = $"SELECT TOP 5 c.id, c.categoryId, c.categoryName, c.sku, c.name, c.price FROM c WHERE c.categoryId = 'C7324EF3-D951-45D9-A345-A82EAE344394'";
			var response1 = await productContainer.GetItemQueryIterator<dynamic>(query1).ReadNextAsync();
			var result1 = response1.ToList();
			var cost1 = response1.RequestCharge;
			result1.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost1, result1.Count);
			Console.WriteLine();

			ShowDemoSubHeader("Rename product category to 'Clothing, Fun Shorts' (trigger change feed for Azure function)");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("await container.ReadItemAsync<dynamic>(\"C7324EF3-D951-45D9-A345-A82EAE344394\", new PartitionKey(\"category\")");
			Console.WriteLine("document.name = \"Clothing, Fun Shorts\";");
			Console.WriteLine("await container.ReplaceItemAsync<dynamic>(\"C7324EF3-D951-45D9-A345-A82EAE344394\", new PartitionKey(\"category\")");
			Console.ResetColor();
			PressKeyToContinue();
			var response2 = await productCategoryContainer.ReadItemAsync<dynamic>("C7324EF3-D951-45D9-A345-A82EAE344394", new PartitionKey("category"));
			var result2 = response2.Resource;
			var cost2 = response2.RequestCharge;
			ShowRuCost(cost2);
			result2.name = "Clothing, Fun Shorts";
			var response3 = (ItemResponse<JObject>)await productCategoryContainer.ReplaceItemAsync(result2, "C7324EF3-D951-45D9-A345-A82EAE344394", new PartitionKey("category"));
			var cost3 = response3.RequestCharge;
			ShowRuCost(cost3, 1, "Replaced");
			Console.WriteLine();

			ShowDemoSubHeader("Retrieve the same products again (category name is updated by Azure function)");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT TOP 5 c.id, c.categoryId, c.categoryName, c.sku, c.name, c.price FROM c");
			Console.WriteLine(" WHERE c.categoryId = 'C7324EF3-D951-45D9-A345-A82EAE344394'");
			Console.ResetColor();
			PressKeyToContinue();
			var query4 = $"SELECT TOP 5 c.id, c.categoryId, c.categoryName, c.sku, c.name, c.price FROM c WHERE c.categoryId = 'C7324EF3-D951-45D9-A345-A82EAE344394'";
			var response4 = await productContainer.GetItemQueryIterator<dynamic>(query4).ReadNextAsync();
			var result4 = response4.ToList();
			var cost4 = response4.RequestCharge;
			result4.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost4, result4.Count);
			Console.WriteLine();
		}

		private static async Task Demo4_QueryingForSalesOrders()
		{
			ShowDemoHeader("Demo 4 - Querying for Sales Orders (webstore-v4)");

			var container = Cosmos.Client.GetContainer("webstore-v4", "customer");

			ShowDemoSubHeader("Retrieve all sales orders for a customer");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT * FROM c");
			Console.WriteLine(" WHERE c.customerId = '46192BCF-E8BB-4140-A0F1-B8764A7941E7' AND c.type = 'salesOrder'");
			Console.ResetColor();
			PressKeyToContinue();
			var query1 = $"SELECT * FROM c WHERE c.customerId = '46192BCF-E8BB-4140-A0F1-B8764A7941E7' AND c.type = 'salesOrder'";
			var response1 = await container.GetItemQueryIterator<dynamic>(query1).ReadNextAsync();
			var result1 = response1.ToList();
			var cost1 = response1.RequestCharge;
			result1.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost1, result1.Count);
			Console.WriteLine();

			ShowDemoSubHeader("Retrieve a customer with all their sales orders");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT * FROM c");
			Console.WriteLine(" WHERE c.customerId = '46192BCF-E8BB-4140-A0F1-B8764A7941E7'");
			Console.WriteLine(" ORDER BY c.type");
			Console.ResetColor();
			PressKeyToContinue();
			var query2 = $"SELECT * FROM c WHERE c.customerId = '46192BCF-E8BB-4140-A0F1-B8764A7941E7' ORDER BY c.type";
			var response2 = await container.GetItemQueryIterator<dynamic>(query2).ReadNextAsync();
			var result2 = response2.ToList();
			var cost2 = response2.RequestCharge;
			result2.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost2, result2.Count);
			Console.WriteLine();
		}

		private static async Task Demo5_QueryingForTopCustomers()
		{
			ShowDemoHeader("Demo 5 - Querying for Top Customers (webstore-v4)");

			var container = Cosmos.Client.GetContainer("webstore-v4", "customer");

			ShowDemoSubHeader("Retrieve the top 5 customers by number of sales orders");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT TOP 5 c.id, c.firstName, c.lastName, c.salesOrderCount");
			Console.WriteLine(" FROM c");
			Console.WriteLine(" WHERE c.type = 'customer'");
			Console.WriteLine(" ORDER BY c.salesOrderCount DESC");
			Console.ResetColor();
			PressKeyToContinue();
			var query1 = $"SELECT TOP 5 c.id, c.firstName, c.lastName, c.salesOrderCount FROM c WHERE c.type = 'customer' ORDER BY c.salesOrderCount DESC";
			var response1 = await container.GetItemQueryIterator<dynamic>(query1).ReadNextAsync();
			var result1 = response1.ToList();
			var cost1 = response1.RequestCharge;
			result1.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost1, result1.Count);
			Console.WriteLine();

			PressKeyToContinue();
			ShowDemoSubHeader("Create a new sales order using a stored procedure");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("dynamic newOrder = new");
			Console.WriteLine("{");
			Console.WriteLine("	id = Guid.NewGuid().ToString(),");
			Console.WriteLine("	type = \"salesOrder\",");
			Console.WriteLine("	customerId = \"44A6D5F6-AF44-4B34-8AB5-21C5DC50926E\",");
			Console.WriteLine("	details = new[]");
			Console.WriteLine("	{");
			Console.WriteLine("		new");
			Console.WriteLine("		{");
			Console.WriteLine("			sku = \"BK-R50R-44\",");
			Console.WriteLine("			name = \"Road-650 Red, 44\",");
			Console.WriteLine("			price = 419.4589,");
			Console.WriteLine("			quantity = 1");
			Console.WriteLine("		},");
			Console.WriteLine("		new");
			Console.WriteLine("		{");
			Console.WriteLine("			sku = \"BK-R68R-52\",");
			Console.WriteLine("			name = \"Road-450 Red, 52\",");
			Console.WriteLine("			price = 874.794,");
			Console.WriteLine("			quantity = 1");
			Console.WriteLine("		}");
			Console.WriteLine("	}");
			Console.WriteLine("};");
			Console.WriteLine();
			Console.WriteLine("await container.Scripts.ExecuteStoredProcedureAsync<dynamic>(");
			Console.WriteLine("  \"spCreateSalesOrder\", new PartitionKey(\"44A6D5F6-AF44-4B34-8AB5-21C5DC50926E\"), new[] { newOrder })");
			Console.ResetColor();
			PressKeyToContinue();
			dynamic newOrder = new
			{
				id = Guid.NewGuid().ToString(),
				type = "salesOrder",
				customerId = "44A6D5F6-AF44-4B34-8AB5-21C5DC50926E",
				details = new[]
				{
					new
					{
						sku = "BK-R50R-44",
						name = "Road-650 Red, 44",
						price = 419.4589,
						quantity = 1
					},
					new
					{
						sku = "BK-R68R-52",
						name = "Road-450 Red, 52",
						price = 874.794,
						quantity = 1
					}
				}
			};
			var response2 = await container.Scripts.ExecuteStoredProcedureAsync<dynamic>("spCreateSalesOrder", new PartitionKey("44A6D5F6-AF44-4B34-8AB5-21C5DC50926E"), new[] { newOrder });
			var cost2 = response2.RequestCharge;
			ShowRuCost(cost2, null, "Executed sproc");
			PressKeyToContinue();

			ShowDemoSubHeader("Retrieve the top 5 customers by number of sales orders");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("SELECT TOP 5 c.id, c.firstName, c.lastName, c.salesOrderCount");
			Console.WriteLine(" FROM c");
			Console.WriteLine(" WHERE c.type = 'customer'");
			Console.WriteLine(" ORDER BY c.salesOrderCount DESC");
			Console.ResetColor();
			PressKeyToContinue();
			var query3 = $"SELECT TOP 5 c.id, c.firstName, c.lastName, c.salesOrderCount FROM c WHERE c.type = 'customer' ORDER BY c.salesOrderCount DESC";
			var response3 = await container.GetItemQueryIterator<dynamic>(query3).ReadNextAsync();
			var result3 = response3.ToList();
			var cost3 = response3.RequestCharge;
			result3.ForEach(r => Console.WriteLine(JsonConvert.SerializeObject(r, Formatting.Indented)));
			ShowRuCost(cost1, result1.Count);
			Console.WriteLine();
		}

		private static async Task ResetDemos()
		{
			ShowDemoHeader("Reset Demos");

			ShowDemoSubHeader("Reset webstore-v3");
			var productCategoryContainer = Cosmos.Client.GetContainer("webstore-v3", "productCategory");
			var product = (await productCategoryContainer.ReadItemAsync<dynamic>("C7324EF3-D951-45D9-A345-A82EAE344394", new PartitionKey("category"))).Resource;
			if (product.name != "Clothing, Shorts")
			{
				product.name = "Clothing, Shorts";
				await productCategoryContainer.ReplaceItemAsync(product, "C7324EF3-D951-45D9-A345-A82EAE344394", new PartitionKey("category"));
				Console.WriteLine("Demo has been reset for webstore-v3");
			}
			else
			{
				Console.WriteLine("Nothing to reset for webstore-v3");
			}

			ShowDemoSubHeader("Reset webstore-v4");
			var customerContainer = Cosmos.Client.GetContainer("webstore-v4", "customer");
			var customerWithOrdersQuery = $"SELECT * FROM c WHERE c.customerId = '44A6D5F6-AF44-4B34-8AB5-21C5DC50926E'";
			var customerWithOrders = (await customerContainer.GetItemQueryIterator<dynamic>(customerWithOrdersQuery).ReadNextAsync()).ToList();
			var customer = customerWithOrders.First(c => c.id == "44A6D5F6-AF44-4B34-8AB5-21C5DC50926E");
			if (customer.salesOrderCount == 29 && customerWithOrders.Count == 30)
			{
				var order = customerWithOrders
					.Where(c => c.type == "salesOrder")
					.OrderByDescending(c => c._ts)
					.First();

				customer.salesOrderCount = 28;
				await customerContainer.ReplaceItemAsync(customer, "44A6D5F6-AF44-4B34-8AB5-21C5DC50926E", new PartitionKey("44A6D5F6-AF44-4B34-8AB5-21C5DC50926E"));
				await customerContainer.DeleteItemAsync<dynamic>(order.id.ToString(), new PartitionKey("44A6D5F6-AF44-4B34-8AB5-21C5DC50926E"));
				Console.WriteLine("Demo has been reset for webstore-v4");
			}
			else
			{
				Console.WriteLine("Nothing to reset for webstore-v4");
			}
			Console.WriteLine();

		}

		private static bool Matches(this string operation, string match) =>
			match.StartsWith(operation);

		private static void ShowUsage()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  d1            Demo 1 - Querying for Customers");
			Console.WriteLine("  d2            Demo 2 - Querying for Product Categories");
			Console.WriteLine("  d3            Demo 3 - Denormalization using the Change Feed");
			Console.WriteLine("  d4            Demo 4 - Querying for Sales Orders");
			Console.WriteLine("  d5            Demo 5 - Querying for Top Customers");
			Console.WriteLine("  reset         Reset demos");
			Console.WriteLine("  help (or ?)   Show usage");
			Console.WriteLine("  quit          Exit demos");
			Console.WriteLine();
		}

		private static void ShowDemoHeader(string text)
		{
			Console.ForegroundColor = ConsoleColor.Black;
			Console.BackgroundColor = ConsoleColor.Yellow;
			Console.Write(text);
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine();
		}

		private static void ShowDemoSubHeader(string text)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(text);
			Console.ResetColor();
			Console.WriteLine();
		}

		private static void ShowRuCost(double cost, int? count = 1, string action = "Retrieved")
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			if (count != null)
			{
				Console.WriteLine($"{action} {count} document(s), cost: {cost} RUs");
			}
			else
			{
				Console.WriteLine($"{action}, cost: {cost} RUs");
			}
			Console.ForegroundColor = ConsoleColor.White;
		}

		private static void PressKeyToContinue()
		{
			Console.Write("Press any key to continue");
			Console.ReadKey();
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.WriteLine("                                        ");
		}

	}
}
