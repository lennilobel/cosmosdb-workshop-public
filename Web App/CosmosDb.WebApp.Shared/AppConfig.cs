namespace CosmosDb.WebApp.Shared
{
	public class AppConfig
    {
        public string CoreApiEndpoint { get; set; }
        public string CoreApiMasterKey { get; set; }
        public string CoreApiWestUsEndpoint { get; set; }
        public string CoreApiWestUsMasterKey { get; set; }

		public string TableApiConnectionString { get; set; }

        public string GremlinApiHostName { get; set; }
        public string GremlinApiMasterKey { get; set; }

        public string MongoDbApiConnectionString { get; set; }

        public string CassandraApiContactPoint { get; set; }
        public int CassandraApiPortNumber { get; set; }
        public string CassandraApiUsername { get; set; }
        public string CassandraApiPassword { get; set; }

        public bool BreakForDemos { get; set; }
    }
}
