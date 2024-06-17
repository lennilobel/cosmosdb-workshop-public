using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CosmosDb.Rag.Embed.Demos
{
	public static class CleanseDatasetDemo
    {
        public static async Task Run()
        {
			Debugger.Break();
			
            var dirty = await File.ReadAllTextAsync("Movies_raw.json");
            var documents = JsonConvert.DeserializeObject<JArray>(dirty);
            var count = documents.Count;
            var started = DateTime.Now;
            var clean = new JArray();

            foreach (JObject document in documents)
            {
                TransformDocument(document);
                clean.Add(document);
            }

            await File.WriteAllTextAsync("..\\..\\..\\Movies.json", clean.ToString());

            Console.WriteLine($"Cleansed {count} documents in {DateTime.Now.Subtract(started)}");
        }

        private static void TransformDocument(JObject document)
        {
            // Set every movie's type to "movie" to put all the movies in the same single logical partition
            document.Add("type", "movie");

            // Remove the "adult" property
            document.Remove("adult");

            // Transform numeric properties
            document["budget"] = Convert.ToInt64(document["budget"].ToString());

            // Transform properties holding valid JSON strings into true JSON objects/arrays
            ConvertPropertyToJToken<JObject>(document, "belongs_to_collection");
            ConvertPropertyToJToken<JArray>(document, "genres");
            ConvertPropertyToJToken<JArray>(document, "production_companies");
            ConvertPropertyToJToken<JArray>(document, "production_countries");
            ConvertPropertyToJToken<JArray>(document, "spoken_languages");
        }

		private static readonly Regex JsonStringPattern = new(@"(?<=:\s*)\""(?<value>.*?)\""", RegexOptions.Compiled);

		public static void ConvertPropertyToJToken<T>(JObject movie, string propertyName) where T : JToken
		{
			var json =
				// Replace double-quoted JSON string values with single-quoted values, converting any single quotes within the values to double quotes.
				JsonStringPattern.Replace(movie[propertyName].ToString(), m => $"'{m.Groups["value"].Value.Replace("'", "\"")}'")
				// Fix bad JSON: None > null
				.Replace("_path': None", "_path': null");

			movie[propertyName] = JToken.Parse(json) as T;
		}

	}
}
