using CosmosDb.WebApp.Shared;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.DataLayer.CoreApi
{
	public static class CoreApiSampleDataRepo
    {
        public static async Task<string> CreateFamiliesContainerForGlobalDistDemos(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

			using (var client = new CosmosClient(config.CoreApiWestUsEndpoint, config.CoreApiWestUsMasterKey))
			{
				var database = await CreateDatabaseWithOverwrite(client, "Families");
				var props = new ContainerProperties
				{
					Id = "Families",
					PartitionKeyPath = "/address/zipCode",
					ConflictResolutionPolicy = new ConflictResolutionPolicy
					{
						Mode = ConflictResolutionMode.Custom,
						ResolutionProcedure = "dbs/Families/colls/Families/sprocs/resolveConflict"
					}
				};
				var container = (await database.CreateContainerAsync(props, 1000)).Container;

				dynamic docDef = new
				{
					id = "Sample",
					familyName = "Jones",
					address = new
					{
						addressLine = "456 Harbor Boulevard",
						city = "Chicago",
						state = "IL",
						zipCode = "60603"
					},
					parents = new string[]
					{
						"David",
						"Diana"
					},
					kids = new string[]
					{
						"Evan"
					},
					pets = new string[]
					{
						"Lint"
					}
				};

				await container.CreateItemAsync(docDef, new PartitionKey("60603"));

				return "Successfully created Families container for Global Distribution demos";
			}
        }

		public static async Task<string> CreateFamiliesContainerForQueryDemos(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var breakForDemos = config.BreakForDemos;
            config.BreakForDemos = false;

            try
            {
				var docs = GenerateFamilyDocuments();

				using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
				{
					var database = await CreateDatabaseWithOverwrite(client, "Families");
					var container = (await database.CreateContainerAsync("Families", "/location/state", 400)).Container;
					foreach (var doc in docs)
					{
						var documentObject = JsonConvert.DeserializeObject<JObject>(doc);
						await container.CreateItemAsync(documentObject, new PartitionKey(documentObject["location"]["state"].Value<string>()));
					}
				}

				return "Successfully created Families container for SQL Query demos";
            }
            finally
            {
                config.BreakForDemos = breakForDemos;
            }
        }

        private static IEnumerable<string> GenerateFamilyDocuments()
        {
            var andersen = @"
                {
	                ""id"": ""AndersenFamily"",
                    ""lastName"": ""Andersen"",
	                ""parents"": [
		                {
			                ""firstName"": ""Thomas"",
			                ""relationship"": ""father""

                        },
		                {
			                ""firstName"": ""Mary Kay"",
			                ""relationship"": ""mother""
		                }
	                ],
	                ""children"": [
		                {
			                ""firstName"": ""Henriette Thaulow"",
			                ""gender"": ""female"",
			                ""grade"": 5,
			                ""pets"": [
				                {
					                ""givenName"": ""Fluffy"",
					                ""type"": ""Rabbit""
				                }
			                ]
		                }
	                ],
	                ""location"": {
		                ""state"": ""WA"",
		                ""county"": ""King"",
		                ""city"": ""Seattle""
	                },
	                ""geo"": {
		                ""type"": ""Point"",
		                ""coordinates"": [ -122.3295, 47.60357 ]
	                },
	                ""isRegistered"": true
                }
            ";

            var smith = @"
                {
	                ""id"": ""SmithFamily"",
                    ""parents"": [
		                {
			                ""familyName"": ""Smith"",
			                ""givenName"": ""James""

                        },
		                {
			                ""familyName"": ""Curtis"",
			                ""givenName"": ""Helen""
		                }
	                ],
	                ""children"": [
		                {
			                ""givenName"": ""Michelle"",
			                ""gender"": ""female"",
			                ""grade"": 1
		                },
		                {
			                ""givenName"": ""John"",
			                ""gender"": ""male"",
			                ""grade"": 7,
			                ""pets"": [
				                {
					                ""givenName"": ""Tweetie"",
					                ""type"": ""Bird""
				                }
			                ]
		                }
	                ],
	                ""location"": {
		                ""state"": ""NY"",
		                ""county"": ""Queens"",
		                ""city"": ""Forest Hills""
	                },
	                ""geo"": {
		                ""type"": ""Point"",
		                ""coordinates"": [ -73.84791, 40.72266 ]
	                },
	                ""isRegistered"": true
                }
            ";

            var wakefield = @"
                {
	                ""id"": ""WakefieldFamily"",

                    ""parents"": [
		                {
			                ""familyName"": ""Wakefield"",
			                ""givenName"": ""Robin""

                        },
		                {
			                ""familyName"": ""Miller"",
			                ""givenName"": ""Ben""
		                }
	                ],
	                ""children"": [
		                {
			                ""familyName"": ""Merriam"",
			                ""givenName"": ""Jesse"",
			                ""gender"": ""female"",
			                ""grade"": 6,
			                ""pets"": [
				                {
					                ""givenName"": ""Charlie Brown"",
					                ""type"": ""Dog""
				                },
				                {
					                ""givenName"": ""Tiger"",
					                ""type"": ""Cat""
				                },
				                {
					                ""givenName"": ""Princess"",
					                ""type"": ""Cat""
				                }
			                ]
		                },
		                {
			                ""familyName"": ""Miller"",
			                ""givenName"": ""Lisa"",
			                ""gender"": ""female"",
			                ""grade"": 3,
			                ""pets"": [
				                {
					                ""givenName"": ""Jake"",
					                ""type"": ""Snake""
				                }
			                ]
		                }
	                ],
	                ""location"": {
		                ""state"": ""NY"",
		                ""county"": ""Manhattan"",
		                ""city"": ""NY""
	                },
	                ""geo"": {
		                ""type"": ""Point"",
		                ""coordinates"": [ -73.992, 40.73100 ]
	                },
	                ""isRegistered"": false
                }
            ";

            var documents = new List<string>()
            {
                andersen,
                smith,
                wakefield
            };

            return documents;
        }

        public static async Task<string> CreateStoresContainerFromSqlServer(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var breakForDemos = config.BreakForDemos;
            config.BreakForDemos = false;

            try
            {
                var started = DateTime.Now;
				using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
				{
					var database = await CreateDatabaseWithOverwrite(client, "adventure-works");
					var container = (await database.CreateContainerAsync("stores", "/address/postalCode", 400)).Container;
					using (var conn = new SqlConnection("data source=.;initial catalog=AdventureWorks2017;integrated security=true;"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = File.ReadAllText(@".\Files\CoreApi\ImportAdventureWorks\ImportAdventureWorks.sql");
                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    dynamic documentDef = new
                                    {
                                        id = rdr["id"],
                                        name = rdr["name"],
                                        address = new
                                        {
                                            addressType = rdr["address.addressType"],
                                            addressLine1 = rdr["address.addressLine1"],
                                            location = new
                                            {
                                                city = rdr["address.location.city"],
                                                stateProvinceName = rdr["address.location.stateProvinceName"],
                                            },
                                            postalCode = rdr["address.postalCode"],
                                            countryRegionName = rdr["address.countryRegionName"]
                                        }
                                    };
									await container.CreateItemAsync(documentDef, new PartitionKey(documentDef.address.postalCode));
                                }
                            }
                        }
                        conn.Close();
                    }
                }
                var elapsed = DateTime.Now.Subtract(started);

                return $"Successfully created mystore container from SQL Server in {elapsed}";
            }
            finally
            {
                config.BreakForDemos = breakForDemos;
            }
        }

        public static async Task<string> CreateStoresContainerFromJson(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var breakForDemos = config.BreakForDemos;
            config.BreakForDemos = false;

            try
            {
                var started = DateTime.Now;
                var json = File.ReadAllText(@".\Files\CoreApi\ImportAdventureWorks\ImportAdventureWorks.json");
                var list = JsonConvert.DeserializeObject<List<JObject>>(json);
				using (var client = new CosmosClient(config.CoreApiEndpoint, config.CoreApiMasterKey))
				{
					var database = await CreateDatabaseWithOverwrite(client, "adventure-works");
					var container = (await database.CreateContainerAsync("stores", "/address/postalCode", 400)).Container;
					foreach (var documentDef in list)
                    {
						await container.CreateItemAsync(documentDef, new PartitionKey(documentDef["address"]["postalCode"].ToString()));
                    }
                }
                var elapsed = DateTime.Now.Subtract(started);

                return $"Successfully created stores container from raw JSON in {elapsed}";
            }
            finally
            {
                config.BreakForDemos = breakForDemos;
            }
        }

		private static async Task<Database> CreateDatabaseWithOverwrite(CosmosClient client, string databaseId)
		{
			try
			{
				await client.GetDatabase(databaseId).DeleteAsync();
			}
			catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
			}
			catch (Exception ex)
			{
				throw ex;
			}

			var database = (await client.CreateDatabaseAsync(databaseId)).Database;

			return database;
		}

	}
}
