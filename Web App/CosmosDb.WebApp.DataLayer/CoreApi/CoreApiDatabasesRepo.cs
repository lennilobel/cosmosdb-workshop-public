using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
	public static class CoreApiDatabasesRepo
    {
        public async static Task<IEnumerable<DatabaseProperties>> GetDatabases(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var databases = new List<DatabaseProperties>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var iterator = client.GetDatabaseQueryIterator<DatabaseProperties>();
				while (iterator.HasMoreResults)
				{
					databases.AddRange(await iterator.ReadNextAsync());
				}
			}

			return databases.OrderBy(d => d.Id);
		}

		public static async Task<DatabaseProperties> CreateDatabase(AppConfig config, string id)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var result = await client.CreateDatabaseIfNotExistsAsync(id);
				var databaseDefinition = result.Resource;

				return databaseDefinition;
			}
		}

		public static async Task DeleteDatabase(AppConfig config, string id)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await client.GetDatabase(id).DeleteAsync();
			}
		}

	}
}
