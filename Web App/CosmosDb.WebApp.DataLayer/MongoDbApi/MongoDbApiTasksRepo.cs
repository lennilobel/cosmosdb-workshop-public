using CosmosDb.WebApp.Shared;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace CosmosDb.WebApp.DataLayer.MongoDbApi
{
	public static class MongoDbApiTasksRepo
    {
        public static string CreateTaskListCollection(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new MongoClient(config.MongoDbApiConnectionString);

            var database = client.GetDatabase("Tasks");
            database.CreateCollection("TaskList");

            return "Created TaskList collection in Tasks database";
        }

        public static object CreateTaskItem(AppConfig config, string name, string category, DateTime date, string[] tags)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new MongoClient(config.MongoDbApiConnectionString);

            var database = client.GetDatabase("Tasks");
            var collection = database.GetCollection<TaskItem>("TaskList");

            var taskItem = new TaskItem
            {
                Name = name,
                Category = category,
                DueDate = date,
                Tags = tags,
            };

            collection.InsertOne(taskItem);

            dynamic result = new
            {
                message = "Created Task item in TaskList collection"
            };

            return result;
        }

        public static IEnumerable<TaskItem> ViewTaskListCollection(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new MongoClient(config.MongoDbApiConnectionString);

            var database = client.GetDatabase("Tasks");
            var collection = database.GetCollection<TaskItem>("TaskList");

            var filter = new BsonDocument();
            var taskItems = collection.Find(filter).ToList();

            return taskItems;
        }

        public static string DeleteTaskListCollection(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new MongoClient(config.MongoDbApiConnectionString);

            var database = client.GetDatabase("Tasks");
            database.DropCollection("TaskList");

            return "Deleted TaskList collection in Tasks database";
        }

        public static string DeleteTasksDatabase(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new MongoClient(config.MongoDbApiConnectionString);

            client.DropDatabase("Tasks");

            return "Deleted Tasks database";
        }

        public static string CreatePolyBaseDemoCollection(AppConfig config)
        {
            if (config.BreakForDemos) System.Diagnostics.Debugger.Break();

            var client = new MongoClient(config.MongoDbApiConnectionString);

            var database = client.GetDatabase("FlightTasks");
            database.CreateCollection("TaskList");

            var collection = database.GetCollection<dynamic>("TaskList");

            dynamic item1 = new
            {
                flightId = 1,
                dueDate = "2020-03-01",
                name = "Task 1/2 for flight 1"
            };
            collection.InsertOne(item1);

            dynamic item2 = new
            {
                flightId = 1,
                dueDate = "2020-03-08",
                name = "Task 2/2 for flight 1"
            };
            collection.InsertOne(item2);

            dynamic item3 = new
            {
                flightId = 3,
                dueDate = "2020-05-09",
                name = "Task for flight 3"
            };
            collection.InsertOne(item3);

            return "Created TaskList collection in FlightTasks database for SQL2019 PolyBase demo";
        }

    }
}
