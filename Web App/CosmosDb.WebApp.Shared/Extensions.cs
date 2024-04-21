using Newtonsoft.Json;

namespace CosmosDb.WebApp.Shared
{
	public static class Extensions
    {
		public static string SerializeToJson(this object serializeObject, Formatting formatting = Formatting.Indented)
		{
			var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
			var json = JsonConvert.SerializeObject(serializeObject, formatting, settings);
			return json;
		}
	}
}
