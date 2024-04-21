using Microsoft.AspNetCore.Mvc;
using System;

namespace CosmosDb.WebApp.WebApi.Controllers
{
	public class PingController
    {
		[HttpGet]
        [Route("api/ping")]
        public string Ping()
        {
            return $"Cosmos DB Demos .NET Web API controller running at {DateTime.Now}";
        }
    }

}
