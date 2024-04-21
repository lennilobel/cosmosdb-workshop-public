using CosmosDb.WebApp.Shared;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.GremlinApi
{
	public static class GremlinApiComicBookRepo
    {
        private const int Port = 443;
        private const string DatabaseName = "GraphDb";
        private const string GraphName = "ComicBook";

        public async static Task<string> PopulateComicBookGraph(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var sb = new StringBuilder();

            var hostname = config.GremlinApiHostName;
            var masterKey = config.GremlinApiMasterKey;
            var username = $"/dbs/{DatabaseName}/colls/{GraphName}";

            var gremlinServer = new GremlinServer(hostname, Port, true, username, masterKey);

            using (var client = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                await client.SubmitAsync("g.V().drop()");
            }

            var gremlinApiAccountName = config.GremlinApiHostName.Split('.')[0];
            var gremlinApiSqlEndpoint = $"https://{gremlinApiAccountName}.documents.azure.com:443/";
            using (var client = new CosmosClient(gremlinApiSqlEndpoint, config.GremlinApiMasterKey))
            {
                await PopulateGraph(client, sb, "Vertices");
                await PopulateGraph(client, sb, "Icons");
                await PopulateGraph(client, sb, "Edges");
            }

            return sb.ToString();
        }

        private static async Task PopulateGraph(CosmosClient client, StringBuilder sb, string typeName)
        {
            var started = DateTime.Now;
            var json = File.ReadAllText($@".\Files\GremlinApi\ComicBook\ComicBook{typeName}.json");
            var list = JsonConvert.DeserializeObject<List<object>>(json);
			var container = client.GetContainer(DatabaseName, GraphName);
            var count = 0;
            foreach (dynamic documentDef in list)
            {
                await container.CreateItemAsync(documentDef);
                count++;
            }
            var elapsed = DateTime.Now.Subtract(started);
            sb.AppendLine($"Populated {count:N0} {typeName} in {elapsed}");
        }

    }
}
