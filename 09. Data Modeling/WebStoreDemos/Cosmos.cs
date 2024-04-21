using Microsoft.Azure.Cosmos;

namespace WebStoreDemos
{
	public static class Cosmos
	{
		private static string _endpoint;
		private static string _masterKey;
		private static CosmosClient _client;

		public static void SetAuth(string endpoint, string masterKey)
		{
			_endpoint = endpoint;
			_masterKey = masterKey;
		}

		public static CosmosClient Client
		{
			get
			{
				if (_client == null)
				{
					_client = new CosmosClient(_endpoint, _masterKey);
				}
				return _client;
			}
		}

	}
}
