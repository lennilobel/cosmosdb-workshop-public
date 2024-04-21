using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
	public static class CoreApiSecurityRepo
	{
		public static async Task<IEnumerable<UserProperties>> GetUsers(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var users = new List<UserProperties>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var iterator = client.GetDatabase("mydb").GetUserQueryIterator<UserProperties>();
				while (iterator.HasMoreResults)
				{
					users.AddRange(await iterator.ReadNextAsync());
				}
			}

			return users.OrderBy(u => u.Id);
		}

		public static async Task<string> CreateUsers(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine(">>> Create Users <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var database = client.GetDatabase("mydb");

				await database.CreateUserAsync("Alice");
				sb.AppendLine("Created user: 'Alice'");

				await database.CreateUserAsync("Tom");
				sb.AppendLine("Created user: 'Tom'");
			}

			return sb.ToString();
		}

		public static async Task<object> CreatePermissions(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();
			sb.AppendLine(">>> Create Permissions <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var alicePerm = await CreatePermission(client, sb, "Alice", "AliceContainerAccess", PermissionMode.All);
				var tomPerm = await CreatePermission(client, sb, "Tom", "TomContainerAccess", PermissionMode.Read);

				dynamic result = new
				{
					alicePerm,
					tomPerm,
					output = sb.ToString(),
				};

				return result;
			}
		}

		private static async Task<PermissionProperties> CreatePermission(CosmosClient client, StringBuilder sb, string userId, string permissionId, PermissionMode permissionMode)
		{
			sb.AppendLine();
			sb.AppendLine($">>> Create Permission {permissionId} for {userId} <<<");

			var database = client.GetDatabase("mydb");
			var container = database.GetContainer("mystore");
			var user = database.GetUser(userId);
			var permResponse = await user.CreatePermissionAsync(new PermissionProperties(permissionId, permissionMode, container));
			var perm = permResponse.Resource;

			sb.AppendLine($"Created new permission '{permissionId}' on '{database.Id}' container ('{container.Id}') with access '{permissionMode}' for user '{userId}'");

			return perm;
		}

		public static async Task<IEnumerable<PermissionProperties>> GetPermissions(AppConfig config, string userId)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var permissions = new List<PermissionProperties>();
			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var database = client.GetDatabase("mydb");
				var user = database.GetUser(userId);
				var iterator = user.GetPermissionQueryIterator<PermissionProperties>();
				while (iterator.HasMoreResults)
				{
					permissions.AddRange(await iterator.ReadNextAsync());
				}
			}

			return permissions.OrderBy(p => p.Id);
		}

		public static async Task<string> TestPermissions(AppConfig config, string userId)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();
			sb.AppendLine(">>> Test Permissions <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				var database = client.GetDatabase("mydb");
				var user = database.GetUser(userId);
				var permission = (await user.GetPermissionQueryIterator<PermissionProperties>().ReadNextAsync()).First();

				sb.AppendLine();
				sb.AppendLine($"Trying to create & delete document as user {userId}");

				var output = await TestPermissions(config.CoreApiEndpoint, permission);
				sb.Append(output);
			}

			return sb.ToString();
		}

		private static async Task<string> TestPermissions(string endpoint, PermissionProperties permission)
		{
			var sb = new StringBuilder();

			dynamic documentDefinition = new
			{
				id = Guid.NewGuid().ToString(),
				name = "New Customer 1",
				address = new
				{
					addressType = "Main Office",
					addressLine1 = "123 Main Street",
					location = new
					{
						city = "Brooklyn",
						stateProvinceName = "New York"
					},
					postalCode = "11229",
					countryRegionName = "United States"
				},
			};

			try
			{
				using (var restrictedClient = new CosmosClient(endpoint, permission.Token))
				{
					var container = restrictedClient.GetContainer("mydb", "mystore");

					await container.CreateItemAsync(documentDefinition, new PartitionKey(documentDefinition.address.postalCode));
					sb.AppendLine($"Successfully created document: {documentDefinition.id}");

					await container.DeleteItemAsync<dynamic>(documentDefinition.id, new PartitionKey(documentDefinition.address.postalCode));
					sb.AppendLine($"Successfully deleted document: {documentDefinition.id}");
				}
			}
			catch (Exception ex)
			{
				sb.AppendLine($"ERROR: {ex.Message}");
			}

			return sb.ToString();
		}

		public static async Task<string> DeleteUsers(AppConfig config)
		{
			if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			var sb = new StringBuilder();

			sb.AppendLine(">>> Delete Users <<<");
			sb.AppendLine();

			using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
			{
				await TryDeleteUser(client, sb, "Alice");
				await TryDeleteUser(client, sb, "Tom");
			}

			return sb.ToString();
		}

		private static async Task TryDeleteUser(CosmosClient client, StringBuilder sb, string userId)
		{
			var database = client.GetDatabase("mydb");

			var user = database.GetUser(userId);
			await user.DeleteAsync();

			sb.AppendLine($"Deleted user: {userId}");
		}

	}
}
