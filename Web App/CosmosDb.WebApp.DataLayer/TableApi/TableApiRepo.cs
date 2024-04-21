using Azure.Data.Tables;
using CosmosDb.WebApp.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.TableApi
{
	public static class TableApiRepo
    {
        public static async Task<string> CreateMoviesTable(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var serviceClient = new TableServiceClient(config.TableApiConnectionString);
            var table = serviceClient.GetTableClient("Movies");

            table.Create();

            var movies = new MovieEntity[]
            {
                new MovieEntity("sci-fi", "Star Wars IV - A New Hope")
                {
                    Year = 1977,
                    Length = "2hr, 1min",
                    Description = "Luke Skywalker joins forces with a Jedi Knight, a cocky pilot, a Wookiee and two droids to save the galaxy from the Empire's world-destroying battle-station while also attempting to rescue Princess Leia from the evil Darth Vader."
                },
                new MovieEntity("sci-fi", "Star Wars V - The Empire Strikes Back")
                {
                    Year = 1980,
                    Length = "2hr, 4min",
                    Description = "After the rebels are overpowered by the Empire on the ice planet Hoth, Luke Skywalker begins Jedi training with Yoda. His friends accept shelter from a questionable ally as Darth Vader hunts them in a plan to capture Luke."
                }
            };

            var batch = new List<TableTransactionAction>();
            batch.AddRange(movies.Select(m => new TableTransactionAction(TableTransactionActionType.Add, m)));

            var response = await table.SubmitTransactionAsync(batch).ConfigureAwait(false);

            var sb = new StringBuilder();
            for (var i = 0; i < movies.Length; i++)
            {
                sb.AppendLine($"The ETag for the entity with RowKey: '{movies[i].RowKey}' is {response.Value[i].Headers.ETag}");
            }

            sb.AppendLine("Created Movies table with 2 movie rows");

            return sb.ToString();
        }

        public static async Task<IEnumerable<MovieEntity>> ViewMovieRows(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var serviceClient = new TableServiceClient(config.TableApiConnectionString);
            var table = serviceClient.GetTableClient("Movies");

            var result = table.QueryAsync<MovieEntity>("PartitionKey eq 'sci-fi'");

            var movies = new List<MovieEntity>();

            // Details on pagination available here: https://docs.microsoft.com/en-us/dotnet/azure/sdk/pagination
            // if you want to extend this in a paged version / understand more.
            await foreach (var page in result.AsPages())
            {
                movies.AddRange(page.Values);
            }

            return movies;
        }

        public static async Task<string> CreateMovieRow(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var serviceClient = new TableServiceClient(config.TableApiConnectionString);
            var table = serviceClient.GetTableClient("Movies");

            var movie = new MovieEntity("sci-fi", "Star Wars VI - Return of the Jedi")
            {
                Year = 1983,
                Length = "2hr, 11min",
                Description = "After a daring mission to rescue Han Solo from Jabba the Hutt, the rebels dispatch to Endor to destroy a more powerful Death Star. Meanwhile, Luke struggles to help Vader back from the dark side without falling into the Emperor's trap."
            };

            var result = await table.UpsertEntityAsync(movie);

            return "Created new movie row";
        }

        public static async Task<string> DeleteMovieRow(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new TableServiceClient(config.TableApiConnectionString);

            var table = client.GetTableClient("Movies");

            await table.DeleteEntityAsync("sci-fi", "Star Wars VI - Return of the Jedi");

            return "Deleted new movie row";
        }

        public static async Task<string> DeleteMoviesTable(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new TableServiceClient(config.TableApiConnectionString);

            var table = client.GetTableClient("Movies");

            await table.DeleteAsync();

            return "Deleted Movies table";
        }

    }
}
