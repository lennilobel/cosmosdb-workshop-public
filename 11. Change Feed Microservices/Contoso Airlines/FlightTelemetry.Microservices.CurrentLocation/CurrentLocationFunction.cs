using FlightTelemetry.Shared;
using FlightTelemetry.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightTelemetry.Microservices.CurrentLocation
{
	public static class CurrentLocationFunction
    {
        private static readonly CosmosClient _client =
            new(Environment.GetEnvironmentVariable("CosmosDbConnectionString"));

        private static readonly Dictionary<string, DateTime> _timestamps =
            new();

        private static readonly object _threadLock =
            new();

        [FunctionName("CurrentLocation")]
        public static async Task CurrentLocation(
            [CosmosDBTrigger(
                databaseName: Constants.DatabaseName,
                containerName: Constants.LocationContainerName,
                Connection = "CosmosDbConnectionString",
                LeaseContainerName = "lease",
                LeaseContainerPrefix = "CurrentLocation-"
            )]
            string documentsJson,
            ILogger logger)
        {
            var container = _client.GetContainer(Constants.DatabaseName, Constants.CurrentLocationContainerName);

            var documents = JsonConvert.DeserializeObject<JArray>(documentsJson);
            foreach (JObject document in documents)
            {
                try
                {
                    await UpdateCurrentLocation(container, document, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }
        }

        private static async Task UpdateCurrentLocation(Container container, JObject document, ILogger logger)
        {
            var locationEvent = JsonConvert.DeserializeObject<LocationEvent>(document.ToString());

            if (ShouldSkip(locationEvent))
            {
                return;
            }

            // Swap the GUID in the document's ID with the flight number to enable point reads
            locationEvent.Id = locationEvent.FlightNumber;

            // Upsert to the currentLocation container
            var result = await container.UpsertItemAsync(locationEvent, new Microsoft.Azure.Cosmos.PartitionKey("location"));

            logger.LogWarning($"Upserted location event to materialized view for flight {locationEvent.FlightNumber} ({result.RequestCharge} RUs)");
        }

        private static bool ShouldSkip(LocationEvent locationEvent)
        {
            if (locationEvent.IsComplete)   // Make sure not to miss the last location event
            {
                return false;
            }
            // Throttle continuous processing by delaying between updates of the same flight to the currentLocation container
            lock (_threadLock)
            {
                if (_timestamps.ContainsKey(locationEvent.FlightNumber))
                {
                    if (DateTime.Now.Subtract(_timestamps[locationEvent.FlightNumber]).TotalSeconds < 3)
                    {
                        return true;
                    }
                    else
                    {
                        _timestamps[locationEvent.FlightNumber] = DateTime.Now;
                    }
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
