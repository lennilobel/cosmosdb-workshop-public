namespace CosmosDb.Rag.Demos
{
	public class AppConfig
	{
		public CosmosDbConfig CosmosDb { get; set; }
		public class CosmosDbConfig
		{
			public string Endpoint { get; set; }
			public string MasterKey { get; set; }
			public string DatabaseName { get; set; }
			public string ContainerName { get; set; }
		}

		public OpenAIConfig OpenAI { get; set; }
		public class OpenAIConfig
		{
			public string Endpoint { get; set; }
			public string ApiKey { get; set; }
			public string EmbeddingsDeploymentName { get; set; }
			public string CompletionsDeploymentName { get; set; }
			public string DalleDeploymentName { get; set; }
		}

	}
}
