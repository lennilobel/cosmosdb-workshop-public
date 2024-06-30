using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;

namespace CosmosDb.Rag.Demos
{
	public static class Shared
	{
		public static AppConfig AppConfig { get; set; }
		public static CosmosClient CosmosClient { get; set; }
		public static OpenAIClient OpenAIClient { get; set; }

		public static void Initialize()
		{
			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
			AppConfig = config.GetSection("AppConfig").Get<AppConfig>();

			var cosmosDb = AppConfig.CosmosDb;
			var openAI = AppConfig.OpenAI;

			CosmosClient = new CosmosClient(cosmosDb.Endpoint, cosmosDb.MasterKey, new CosmosClientOptions { AllowBulkExecution = true });
			OpenAIClient = new OpenAIClient(new Uri(openAI.Endpoint), new AzureKeyCredential(openAI.ApiKey));
		}

	}
}
