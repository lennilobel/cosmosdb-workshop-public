using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace CosmosDb.DotNetSdk.Demos
{
	public static class Shared
	{
		public static CosmosClient Client { get; private set; }

		static Shared()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var endpoint = config["CosmosEndpoint"];
            var masterKey = config["CosmosMasterKey"];

			Client = new CosmosClient(endpoint, masterKey);
		}

		public static async Task DeleteContainerIfExists(string databaseName, string containerName)
		{
			try
			{
				await Client.GetContainer(databaseName, containerName).DeleteContainerAsync();
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
			}
		}

	}
}
