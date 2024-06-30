using System.Threading.Tasks;

namespace CosmosDb.Rag.Assistant
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			Shared.Initialize();

			var assistant = new MoviesAssistantDemo();
			await assistant.Run();
		}
	}
}
