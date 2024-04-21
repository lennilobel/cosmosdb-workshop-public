using CosmosDb.WebApp.DataLayer;
using CosmosDb.WebApp.DataLayer.CassandraApi;
using CosmosDb.WebApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace CosmosDb.WebApp.WebApi.Controllers
{
	public class CassandraApiController
    {
        private readonly AppConfig _config;

        public CassandraApiController(IOptions<AppConfig> config)
        {
            this._config = config.Value;
        }

        [HttpGet]
        [Route("api/cassandra/movies/create")]
        public string CreateMoviesTable()
        {
            var message = CassandraApiRepo.CreateMoviesTable(this._config);
            return message;
        }

        [HttpGet]
        [Route("api/cassandra/movies/rows/query/all")]
        public IEnumerable<Movie> QueryAllMovies()
        {
            var movies = CassandraApiRepo.QueryAllMovies(this._config);
            return movies;
        }

        [HttpGet]
        [Route("api/cassandra/movies/rows/query/scifi")]
        public IEnumerable<Movie> QuerySciFiMovies()
        {
            var movies = CassandraApiRepo.QuerySciFiMovies(this._config);
            return movies;
        }

        [HttpGet]
        [Route("api/cassandra/movies/rows/query/starWarsVI")]
        public IEnumerable<Movie> QueryStarWarsVI()
        {
            var movies = CassandraApiRepo.QueryStarWarsVI(this._config);
            return movies;
        }

        [HttpGet]
        [Route("api/cassandra/movies/rows/create")]
        public string CreateMovieRow()
        {
            var message = CassandraApiRepo.CreateMovieRow(this._config);
            return message;
        }

        [HttpGet]
        [Route("api/cassandra/movies/rows/delete")]
        public string DeleteMovieRow()
        {
            var message = CassandraApiRepo.DeleteMovieRow(this._config);
            return message;
        }

        [HttpGet]
        [Route("api/cassandra/movies/delete")]
        public string DeleteMoviesTable()
        {
            var message = CassandraApiRepo.DeleteMoviesTable(this._config);
            return message;
        }

    }

}
