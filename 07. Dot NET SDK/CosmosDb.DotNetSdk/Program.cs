using CosmosDb.DotNetSdk.Demos;
using CosmosDb.DotNetSdk.Demos.Batch;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.DotNetSdk
{
	public static class Program
	{
		private static IDictionary<string, Func<Task>> DemoMethods;

		private static void Main(string[] args)
		{
			DemoMethods = new Dictionary<string, Func<Task>>
			{
				{ "DB", DatabasesDemo.Run },
				{ "CO", ContainersDemo.Run },
				{ "DO", DocumentsDemo.Run },
				{ "IX", IndexingDemo.Run },
				{ "BX", BulkDemo.Run },
				{ "TB", BatchDemo.Run },
				{ "C", Cleanup.Run }
			};

			Task.Run(async () =>
			{
				ShowMenu();
				while (true)
				{
					Console.Write("Selection: ");
					var input = Console.ReadLine();
					var demoId = input.ToUpper().Trim();
					if (DemoMethods.Keys.Contains(demoId))
					{
						var demoMethod = DemoMethods[demoId];
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
			Console.WriteLine(@"Cosmos DB NoSQL API .NET SDK demos

DB Databases
CO Containers
DO Documents
IX Indexing
BX Bulk execution
TB Transactional batch

C  Cleanup

Q  Quit
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

	}
}
