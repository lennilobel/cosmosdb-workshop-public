using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
	public static class CoreApiIndexingRepo
	{
		public async static Task<string> IndexingPathExclusions(AppConfig config)
		{
			var sb = new StringBuilder();
			sb.AppendLine(">>> Exclude Index Paths <<<");
			sb.AppendLine();

			var containerProps = new ContainerProperties
			{
				Id = "customindexing",
				PartitionKeyPath = "/zipCode",
			};

			// Exclude everything under /miscellaneous from indexing, except for /miscellaneous/rating
			containerProps.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
			containerProps.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/miscellaneous/*" });
			containerProps.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/miscellaneous/rating/?" });

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await client.GetDatabase("mydb").CreateContainerAsync(containerProps, 400);

				// Load documents 
				var container = client.GetContainer("mydb", "customindexing");

				for (var i = 1; i <= 100; i++)
				{
					dynamic doc = new
					{
						id = Guid.NewGuid().ToString(),
						zipCode = "12345",
						title = $"Document {i}",
						rating = i,
						miscellaneous = new
						{
							title = $"Document {i}",
							rating = i,
						}
					};
					await container.CreateItemAsync(doc, new PartitionKey(doc.zipCode));
				}

				// Querying on indexed properties is most efficient

				var sql = "SELECT * FROM c WHERE c.title = 'Document 90'";
				var result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				sb.AppendLine($"Query indexed string property     Cost = {result.RequestCharge} RUs");

				sql = "SELECT * FROM c WHERE c.rating = 90";
				result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				sb.AppendLine($"Query indexed number property     Cost = {result.RequestCharge} RUs");
				sb.AppendLine();

				// Querying on unindexed properties requires a sequential scan, and costs more RUs

				sql = "SELECT * FROM c WHERE c.miscellaneous.title = 'Document 90'";
				result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				sb.AppendLine($"Query unindexed string property   Cost = {result.RequestCharge} RUs");

				// Excluded property that was explictly included gets indexed

				sql = "SELECT * FROM c WHERE c.miscellaneous.rating = 90";
				result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				sb.AppendLine($"Query indexed number property     Cost = {result.RequestCharge} RUs");
				sb.AppendLine();

				// Sorting on indexed properties is supported

				sql = "SELECT * FROM c ORDER BY c.title";
				result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				var docs = result.ToList();
				sb.AppendLine($"Sort on indexed string property   Cost = {result.RequestCharge} RUs");

				sql = "SELECT * FROM c ORDER BY c.rating";
				result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				docs = result.ToList();
				sb.AppendLine($"Sort on indexed number property   Cost = {result.RequestCharge} RUs");
				sb.AppendLine();

				// Sorting on unindexed properties is not supported

				sql = "SELECT * FROM c ORDER BY c.miscellaneous.title";
				try
				{
					result = await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync();
				}
				catch (Exception ex)
				{
					sb.AppendLine($"Sort on unindexed property failed");
					sb.AppendLine(ex.Message);
				}

				// Delete the container
				await container.DeleteContainerAsync();
			}

			return sb.ToString();
		}

		public async static Task<string> IndexingCompositePaths(AppConfig config)
		{
			var sb = new StringBuilder();
			sb.AppendLine(">>> Composite Indexes <<<");
			sb.AppendLine();

			var sql = @"
				SELECT TOP 20 *
				FROM c
				WHERE c.address.countryRegionName = 'United States'
				ORDER BY
					c.address.location.stateProvinceName,
					c.address.location.city,
					c.name
			";

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var container = client.GetContainer("mydb", "mystore");

				// Query won't work without explicitly defined composite indexes
				sb.AppendLine("Multi-property ORDER BY without composite indexes");
				try
				{
					var page1 = await (container.GetItemQueryIterator<dynamic>(sql)).ReadNextAsync();
				}
				catch (Exception ex)
				{
					sb.AppendLine(ex.Message);
					sb.AppendLine();
				}

				// Retrieve the container's current indexing policy
				var response = await container.ReadContainerAsync();
				var containerProperties = response.Resource;

				// Add composite indexes to the indexing policy
				var compositePaths = new Collection<CompositePath>
				{
					new CompositePath { Path = "/address/location/stateProvinceName", Order = CompositePathSortOrder.Ascending },
					new CompositePath { Path = "/address/location/city", Order = CompositePathSortOrder.Ascending },
					new CompositePath { Path = "/name", Order = CompositePathSortOrder.Ascending },
				};
				containerProperties.IndexingPolicy.CompositeIndexes.Add(compositePaths);
				await container.ReplaceContainerAsync(containerProperties);

				// The query works now
				sb.AppendLine("Multi-property ORDER BY with composite indexes");
				var page = await (container.GetItemQueryIterator<Customer>(sql)).ReadNextAsync();
				foreach (var doc in page)
				{
					sb.AppendLine($"{doc.Name,-42}{doc.Address.Location.StateProvinceName,-12}{doc.Address.Location.City,-30}");
				}

				// Remove composite indexes from the indexing policy
				containerProperties.IndexingPolicy.CompositeIndexes.Clear();
				await container.ReplaceContainerAsync(containerProperties);
			}

			return sb.ToString();
		}

		public async static Task<string> IndexingSpatialPaths(AppConfig config)
		{
			var sb = new StringBuilder();
			sb.AppendLine(">>> Spatial Indexes <<<");
			sb.AppendLine();

			var containerDef = new ContainerProperties
			{
				Id = "spatialindexing",
				PartitionKeyPath = "/state",
			};

			// Add a spatial index for the point data in the GeoJSON property /geo1
			var geoPath = new SpatialPath { Path = "/geo1/?" };
			geoPath.SpatialTypes.Add(SpatialType.Point);
			containerDef.IndexingPolicy.SpatialIndexes.Add(geoPath);

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await client.GetDatabase("mydb").CreateContainerAsync(containerDef, 1000);
				var container = client.GetContainer("mydb", "spatialindexing");

				for (var i = 1; i <= 1000; i++)
				{
					var longitude = (i % 100 == 0 ? -73.992 : -119.417931);
					var latitude = (i % 100 == 0 ? 40.73104 : 36.778259);
					var state = (i % 100 == 0 ? "NY" : "CA");
					dynamic doc = new
					{
						id = Guid.NewGuid().ToString(),
						title = $"Document {i}",
						state,
						geo1 = new
						{
							type = "Point",
							coordinates = new[] { longitude, latitude },
						},
						geo2 = new
						{
							type = "Point",
							coordinates = new[] { longitude, latitude },
						},
					};
					await container.CreateItemAsync(doc, new PartitionKey(doc.state));
				}

				var sql = @"
				SELECT * FROM c WHERE
				 ST_DISTANCE(c.geo1, {
				   'type': 'Point',
				   'coordinates': [-73.992, 40.73104]
				 }) <= 10";

				var result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				var list = result.ToList();
				sb.AppendLine($"Query indexed spatial property    Cost = {result.RequestCharge} RUs for {list.Count} results");

				sql = @"
				SELECT * FROM c WHERE
				 ST_DISTANCE(c.geo2, {
				   'type': 'Point',
				   'coordinates': [-73.992, 40.73104]
				 }) <= 10";

				result = await container.GetItemQueryIterator<dynamic>(sql).ReadNextAsync();
				list = result.ToList();
				sb.AppendLine($"Query unindexed spatial property  Cost = {result.RequestCharge} RUs for {list.Count} results");

				// Delete the container
				await container.DeleteContainerAsync();
			}
			return sb.ToString();
		}

	}
}
