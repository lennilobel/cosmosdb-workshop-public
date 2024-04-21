using CosmosDb.WebApp.DataLayer;
using CosmosDb.WebApp.DataLayer.MongoDbApi;
using CosmosDb.WebApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace CosmosDb.WebApp.WebApi.Controllers
{
	public class MongoDbApiController
    {
        private readonly AppConfig _config;

        public MongoDbApiController(IOptions<AppConfig> config)
        {
            this._config = config.Value;
        }

        [HttpGet]
        [Route("api/mongodb/tasklist/create")]
        public string CreateTaskListCollection()
        {
            var message = MongoDbApiTasksRepo.CreateTaskListCollection(this._config);
            return message;
        }

        [HttpPost]
        [Route("api/mongodb/task")]
        public object CreateTaskItem([FromBody] CreateTaskItemRequest request)
        {
            var response = MongoDbApiTasksRepo.CreateTaskItem(this._config, request.Name, request.Category, request.DueDate, request.Tags.Split(','));
            return response;
        }

        [HttpGet]
        [Route("api/mongodb/tasklist/view")]
        public object ViewTaskListCollection()
        {
            var response = MongoDbApiTasksRepo.ViewTaskListCollection(this._config);
            return response;
        }

        [HttpGet]
        [Route("api/mongodb/tasklist/delete")]
        public string DeleteTaskListCollection()
        {
            var message = MongoDbApiTasksRepo.DeleteTaskListCollection(this._config);
            return message;
        }

        [HttpGet]
        [Route("api/mongodb/tasks/delete")]
        public string DeleteTasksDatabase()
        {
            var message = MongoDbApiTasksRepo.DeleteTasksDatabase(this._config);
            return message;
        }

        [HttpGet]
        [Route("api/mongodb/polyBaseDemo/create")]
        public string CreatePolyBaseDemoCollection()
        {
            var message = MongoDbApiTasksRepo.CreatePolyBaseDemoCollection(this._config);
            return message;
        }

    }

    public class CreateTaskItemRequest
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public DateTime DueDate { get; set; }
        public string Tags { get; set; }
    }

}
