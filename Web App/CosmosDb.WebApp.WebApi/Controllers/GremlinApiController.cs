using CosmosDb.WebApp.DataLayer;
using CosmosDb.WebApp.DataLayer.GremlinApi;
using CosmosDb.WebApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.WebApi.Controllers
{
	public class GremlinApiController
    {
        private readonly AppConfig _config;

        public GremlinApiController(IOptions<AppConfig> config)
        {
            this._config = config.Value;
        }

        [HttpGet]
        [Route("api/gremlin/airport/populate")]
        public async Task<string> CreateAirportGraph()
        {
            var response = await GremlinApiAirportRepo.PopulateAirportGraph(this._config);
            return response;
        }

        [HttpGet]
        [Route("api/gremlin/airport/query")]
        public async Task<string> QueryAirportGraph()
        {
            var response = await GremlinApiAirportRepo.QueryAirportGraph(this._config);
            return response;
        }

        [HttpGet]
        [Route("api/gremlin/airport/gates")]
        public async Task<object> GetGates()
        {
            var response = await GremlinApiAirportRepo.GetGates(this._config);
            return response;
        }

        [HttpPost]
        [Route("api/gremlin/airport/query")]
        public async Task<object> QueryAirportGraph([FromBody] AirportQueryRequest request)
        {
            //var request = new AirportQueryRequest
            //{
            //    ArrivalGate = jrequest["ArrivalGate"].Value<string>(),
            //    DepartureGate = jrequest["DepartureGate"].Value<string>(),
            //    FirstSwitchTerminalsThenEat = jrequest["FirstSwitchTerminalsThenEat"].Value<bool>(),
            //    MinYelpRating = jrequest["MinYelpRating"].Value<int>(),
            //};

            var response = await GremlinApiAirportRepo.QueryAirportGraph(this._config, request);
            return response;
        }

        //      [HttpPost]
        //      [Route("api/gremlin/airport/query2")]
        //      public async Task<IEnumerable<AirportQueryResult>> QueryAirportGraph2([FromBody] AirportQueryRequest request)
        //      {
        //          var response = await GremlinApiAirportRepo.QueryAirportGraph2(this._config, request);
        //          return response;
        //      }

        //      [HttpGet]
        //      [Route("api/gremlin/airport/restaurants")]
        //      public async Task<IEnumerable<RestaurantInfo>> GetRestaurants()
        //      {
        //          var response = await GremlinApiAirportRepo.GetRestaurants(this._config);
        //          return response;
        //      }

        [HttpGet]
        [Route("api/gremlin/comicbook/populate")]
        public async Task<string> CreateComicBookGraph()
        {
            var response = await GremlinApiComicBookRepo.PopulateComicBookGraph(this._config);
            return response;
        }

    }

}
