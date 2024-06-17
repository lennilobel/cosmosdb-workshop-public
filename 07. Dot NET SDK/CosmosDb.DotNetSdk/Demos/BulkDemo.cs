using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.DotNetSdk.Demos
{
    public static class BulkDemo
    {
        public static async Task Run()
        {
            Console.WriteLine();

            Console.Write("Document count: ");
            var count = Convert.ToInt32(Console.ReadLine());

            await CreateContainer();

            await RunWithoutBulkDemo(count);
            await RunWithBulkDemo(count);

            await DeleteContainer();
        }

        private static async Task CreateContainer()
        {
            Console.WriteLine();
            Console.WriteLine("Creating bulk-demo container");

            await Shared.DeleteContainerIfExists("adventure-works", "bulk-demo");

            var containerDef = new ContainerProperties
            {
                Id = "bulk-demo",
                PartitionKeyPath = "/pk",
            };

            var database = Shared.Client.GetDatabase("adventure-works");
            await database.CreateContainerAsync(containerDef, 10000);
        }

        private static async Task RunWithoutBulkDemo(int count)
        {
            Console.WriteLine();
            Console.WriteLine(">>> Non-bulk (insert one at a time) <<<    press any key to start...");
            Console.ReadKey(true);
            Console.WriteLine();

            var items = GenerateItems(count);

            var cost = 0D;
            var errors = 0;
            var started = DateTime.Now;
            var container = Shared.Client.GetContainer("adventure-works", "bulk-demo");

            foreach (var item in items)
            {
                try
                {
                    var result = await container.CreateItemAsync(item, new PartitionKey(item.pk));
                    cost += result.RequestCharge;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating document: {ex.Message}");
                    errors++;
                }
            }

            Console.WriteLine($"Created {count - errors} documents (non-bulk): {cost:0.##} RUs in {DateTime.Now.Subtract(started)}");
        }

        private static async Task RunWithBulkDemo(int count)
        {
            Console.WriteLine();
            Console.WriteLine(">>> Bulk inserts <<<    press any key to start...");
            Console.ReadKey(true);
            Console.WriteLine();

            var items = GenerateItems(count);

            var cost = 0D;
            var errors = 0;
            var started = DateTime.Now;
            var container = Shared.Client.GetContainer("adventure-works", "bulk-demo");

            var tasks = new List<Task>(count);
            foreach (var item in items)
            {
                var task = container.CreateItemAsync(item, new PartitionKey(item.pk));
                tasks.Add(task
                    .ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            cost += t.Result.RequestCharge;
                        }
                        else
                        {
                            Console.WriteLine($"Error creating document: {t.Exception.Message}");
                            errors++;
                        }
                    }));
            }
            await Task.WhenAll(tasks);

            Console.WriteLine($"Created {count - errors} documents (bulk): {cost:0.##} RUs in {DateTime.Now.Subtract(started)}");
        }

        private static async Task DeleteContainer()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to delete the container");
            Console.ReadKey(true);
            Console.WriteLine("Deleting bulk-demo container");

            var container = Shared.Client.GetContainer("adventure-works", "bulk-demo");
            await container.DeleteContainerAsync();
        }

        private static Item[] GenerateItems(int count)
        {
            var items = new Item[count];
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid().ToString();
                items[i] = new Item
                {
                    id = id,
                    pk = id,
                    username = $"user{i}"
                };
            }
            
            // Simulate a duplicate to cause an error inserting a document
            items[1].id = items[0].id;
            items[1].pk = items[0].pk;

            return items;
        }
    }

    public class Item
    {
        public string id { get; set; }
        public string pk { get; set; }
        public string username { get; set; }
    }
}
