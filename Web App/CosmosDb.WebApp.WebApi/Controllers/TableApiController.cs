using CosmosDb.WebApp.DataLayer.TableApi;
using CosmosDb.WebApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.WebApi.Controllers
{
	public class TableApiController
    {
        private readonly AppConfig _config;

		public TableApiController(IOptions<AppConfig> config)
		{
			this._config = config.Value;
		}

        [HttpGet]
        [Route("api/table/movies/create")]
        public async Task<string> CreateMoviesTable()
        {
            var response = await TableApiRepo.CreateMoviesTable(this._config);
            return response;
        }

        [HttpGet]
        [Route("api/table/movies/rows/view")]
        public async Task<IEnumerable<MovieEntity>> ViewMovieRows()
        {
            var movies = await TableApiRepo.ViewMovieRows(this._config);
            return movies;
        }

        [HttpGet]
        [Route("api/table/movies/rows/create")]
        public async Task<string> CreateMovieRow()
        {
            var response = await TableApiRepo.CreateMovieRow(this._config);
            return response;
        }

        [HttpGet]
        [Route("api/table/movies/rows/delete")]
        public async Task<string> DeleteMovieRow()
        {
            var response = await TableApiRepo.DeleteMovieRow(this._config);
            return response;
        }

        [HttpGet]
        [Route("api/table/movies/delete")]
        public async Task<string> DeleteMoviesTable()
        {
            var response = await TableApiRepo.DeleteMoviesTable(this._config);
            return response;
        }

    }
}
