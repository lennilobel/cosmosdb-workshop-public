using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
	public static class CoreApiConflictsRepo
	{
		public static async Task<IEnumerable<object>> GetConflicts(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var results = new List<object>();

			using (var client = new CosmosClient(config.CoreApiWestUsEndpoint, config.CoreApiWestUsMasterKey))
			{
				var container = client.GetContainer("Families", "Families");

				var iterator = container.Conflicts.GetConflictQueryIterator<ConflictProperties>();
				while (iterator.HasMoreResults)
				{
					var conflicts = await iterator.ReadNextAsync();
					foreach (var conflictProps in conflicts)
					{
						var loser = container.Conflicts.ReadConflictContent<dynamic>(conflictProps);
						results.Add(new
						{
							conflictProps,
							loser.id,
							partitionKey = loser.address.zipCode,
						});
					}
				}
			}

			return results;
		}

		public static async Task<object> GetConflict(AppConfig config, ConflictProperties conflictProps)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiWestUsEndpoint, config.CoreApiWestUsMasterKey))
			{
				var container = client.GetContainer("Families", "Families");

				var loser = container.Conflicts.ReadConflictContent<dynamic>(conflictProps);

				var winnerResponse = await container.ReadItemAsync<dynamic>(loser.id.ToString(), new PartitionKey(loser.address.zipCode.ToString()));
				var winner = winnerResponse.Resource;

				return new
				{
					loser,
					winner,
				};
			}
		}

		public static async Task<object> ResolveConflict(AppConfig config, ConflictProperties conflictProps)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiWestUsEndpoint, config.CoreApiWestUsMasterKey))
			{
				var container = client.GetContainer("Families", "Families");

				var loser = container.Conflicts.ReadConflictContent<dynamic>(conflictProps);

				await container.Conflicts.DeleteAsync(conflictProps, new PartitionKey(loser.address.zipCode.ToString()));
			}

			return new { message = "Conflict resolved" };
		}

		public static async Task<object> ReverseConflict(AppConfig config, ConflictProperties conflictProps)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiWestUsEndpoint, config.CoreApiWestUsMasterKey))
			{
				var container = client.GetContainer("Families", "Families");

				var loser = container.Conflicts.ReadConflictContent<dynamic>(conflictProps);
				var pk = new PartitionKey(loser.address.zipCode.ToString());

				await container.ReplaceItemAsync<dynamic>(loser, loser.id.ToString(), pk);

				await container.Conflicts.DeleteAsync(conflictProps, pk);
			}

			return new { message = "Conflict reversed" };
		}

	}
}
