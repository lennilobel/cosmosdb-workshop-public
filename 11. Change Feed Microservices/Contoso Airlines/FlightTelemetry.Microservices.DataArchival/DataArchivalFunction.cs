using Azure.Storage.Blobs;
using FlightTelemetry.Shared;
using FlightTelemetry.Shared.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FlightTelemetry.Microservices.DataArchival
{
	public static class DataArchivalFunction
	{
		private static readonly Dictionary<string, DateTime> _timestamps =
			new();

		private static readonly object _threadLock =
			new();

		private static readonly BlobContainerClient _blobContainer;

		static DataArchivalFunction()
		{
			var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("StorageAccountConnectionString"));
			_blobContainer = blobClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("BlobContainerName"));
		}

		[FunctionName("DataArchival")]
		public static async Task DataArchival(
			[CosmosDBTrigger(
				databaseName: Constants.DatabaseName,
				containerName: Constants.LocationContainerName,
				Connection = "CosmosDbConnectionString",
				LeaseContainerName = "lease",
				LeaseContainerPrefix = "DataArchival-"
			)]
			string documentsJson,
			ILogger logger)
		{
			var documents = JsonConvert.DeserializeObject<JArray>(documentsJson);
			foreach (JObject document in documents)
			{
				try
				{
					await ArchiveFlightLocation(document, logger);
				}
				catch (Exception ex)
				{
					logger.LogError($"Error processing document id {document["id"]}: {ex.Message}");
				}
			}
		}

		private static async Task ArchiveFlightLocation(JObject document, ILogger logger)
		{
			var locationEvent = JsonConvert.DeserializeObject<LocationEvent>(document.ToString());
			if (ShouldSkip(locationEvent))
			{
				return;
			}

			var blobName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{locationEvent.FlightNumber}-{locationEvent.Id}.json";
			var blob = _blobContainer.GetBlobClient(blobName);
			var bytes = Encoding.ASCII.GetBytes(document.ToString());
			var data = new BinaryData(bytes);

			await blob.UploadAsync(data);
			
			_timestamps[locationEvent.FlightNumber] = DateTime.Now;
			logger.LogWarning($"Archived '{blobName}' to blob storage");
		}

		private static bool ShouldSkip(LocationEvent locationEvent)
		{
			// Make sure not to miss the last archive for completed flights
			if (locationEvent.IsComplete)
			{
				return false;
			}

			// Throttle continuous processing by delaying between archivals of the same flight to blob storage
			lock (_threadLock)
			{
				if (_timestamps.ContainsKey(locationEvent.FlightNumber))
				{
					if (DateTime.Now.Subtract(_timestamps[locationEvent.FlightNumber]).TotalSeconds < 15)
					{
						return true;
					}
					_timestamps[locationEvent.FlightNumber] = DateTime.Now;
				}
				else
				{
					_timestamps.Add(locationEvent.FlightNumber, DateTime.Now);
				}
			}

			return false;
		}

	}
}
