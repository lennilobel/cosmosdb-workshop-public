using CosmosDb.WebApp.DataLayer.CoreApi;
using CosmosDb.WebApp.Shared;
using CosmosDb.WebApp.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb.WebApp.WebApi.Controllers
{
	public class CoreApiController
    {
        private readonly AppConfig _config;

        public CoreApiController(IOptions<AppConfig> config)
        {
            this._config = config.Value;
        }

        #region "Create Sample Data"

        [HttpGet]
        [Route("api/core/familiesContainer/create/queryDemos")]
        public async Task<string> CreateFamiliesCollectionForQueryDemos() =>
            await CoreApiSampleDataRepo.CreateFamiliesContainerForQueryDemos(this._config);

        [HttpGet]
        [Route("api/core/familiesContainer/create/globalDistDemos")]
        public async Task<string> CreateFamiliesCollectionForGlobalDistDemos() =>
			await CoreApiSampleDataRepo.CreateFamiliesContainerForGlobalDistDemos(this._config);

        [HttpGet]
        [Route("api/core/storesContainer/create/fromSqlServer")]
        public async Task<string> CreateStoresContainerFromSqlServer() =>
            await CoreApiSampleDataRepo.CreateStoresContainerFromSqlServer(this._config);

        [HttpGet]
        [Route("api/core/storesContainer/create/fromJson")]
        public async Task<string> CreateStoresContrainerFromJson() =>
            await CoreApiSampleDataRepo.CreateStoresContainerFromJson(this._config);

        #endregion

        #region "Databases"

        [HttpGet]
        [Route("api/core/databases")]
        public async Task<IEnumerable<object>> GetDatabases() =>
            await CoreApiDatabasesRepo.GetDatabases(this._config);

        [HttpPost]
        [Route("api/core/databases")]
        public async Task<object> CreateDatabase([FromBody] CreateDatabaseRequest request) =>
            await CoreApiDatabasesRepo.CreateDatabase(this._config, request.DatabaseId);

        [HttpDelete]
        [Route("api/core/databases/{databaseId}")]
        public async Task DeleteDatabase(string databaseId) =>
            await CoreApiDatabasesRepo.DeleteDatabase(this._config, databaseId);

        #endregion

        #region "Containers"

        [HttpGet]
        [Route("api/core/containers/{databaseId}")]
        public async Task<IEnumerable<object>> GetContainers(string databaseId) =>
			await CoreApiContainersRepo.GetContainers(this._config, databaseId);

        [HttpPost]
        [Route("api/core/containers")]
        public async Task<object> CreateContainer([FromBody] CreateContainerRequest request) =>
            await CoreApiContainersRepo.CreateContainer(this._config, request.DatabaseId, request.ContainerId, request.PartitionKey, request.Throughput);

        [HttpDelete]
        [Route("api/core/containers/{databaseId}/{containerId}")]
        public async Task DeleteContainer(string databaseId, string containerId) =>
            await CoreApiContainersRepo.DeleteContainer(this._config, databaseId, containerId);

        #endregion

        #region "Documents"

        [HttpGet]
        [Route("api/core/documents/create/{demoType}/{databaseId}/{containerId}")]
        public async Task<object> CreateDocument(string demoType, string databaseId, string containerId)
        {
            switch (demoType)
            {
                case "dynamic":
                    return await CoreApiDocumentsRepo.CreateDocumentFromDynamic(this._config, databaseId, containerId);

                case "json":
					return await CoreApiDocumentsRepo.CreateDocumentFromJson(this._config, databaseId, containerId);

                case "poco":
					return await CoreApiDocumentsRepo.CreateDocumentFromPoco(this._config, databaseId, containerId);
            }

            throw new Exception($"Unsupported demo type: {demoType}");
        }

        [HttpGet]
        [Route("api/core/documents/query/{demoType}/{databaseId}/{containerId}")]
        public async Task<IEnumerable<object>> QueryDocuments(string demoType, string databaseId, string containerId)
        {
            switch (demoType)
            {
                case "dynamic":
					return await CoreApiDocumentsRepo.QueryForDynamic(this._config, databaseId, containerId);

                case "poco":
                    return await CoreApiDocumentsRepo.QueryForPoco(this._config, databaseId, containerId);

                case "linq":
                    return CoreApiDocumentsRepo.QueryWithLinq(this._config, databaseId, containerId);
            }

            throw new Exception($"Unsupported demo type: {demoType}");
        }

        [HttpGet]
        [Route("api/core/documents/queryFirstPage/{databaseId}/{containerId}")]
        public async Task<PagedResult> QueryFirstPage(string databaseId, string containerId) =>
            await CoreApiDocumentsRepo.QueryWithPaging(this._config, databaseId, containerId);

        [HttpPost]
        [Route("api/core/documents/queryNextPage/{databaseId}/{containerId}")]
        public async Task<PagedResult> QueryNextPage(string databaseId, string containerId, [FromBody] object continuationToken) =>
            await CoreApiDocumentsRepo.QueryWithPaging(this._config, databaseId, containerId, continuationToken?.ToString().Replace("\r\n", string.Empty));

        [HttpGet]
        [Route("api/core/documents/replace/{databaseId}/{containerId}")]
        public async Task<string> ReplaceDocuments(string databaseId, string containerId) =>
            await CoreApiDocumentsRepo.ReplaceDocuments(this._config, databaseId, containerId);

        [HttpGet]
        [Route("api/core/documents/delete/{databaseId}/{containerId}")]
        public async Task<string> DeleteDocuments(string databaseId, string containerId) =>
            await CoreApiDocumentsRepo.DeleteDocuments(this._config, databaseId, containerId);

        #endregion

		#region "Conflicts"

		[HttpGet]
		[Route("api/core/conflicts")]
		public async Task<IEnumerable<object>> GetConflicts() =>
			await CoreApiConflictsRepo.GetConflicts(this._config);

		[HttpPost]
		[Route("api/core/conflict")]
		public async Task<object> GetConflict([FromBody] ConflictProperties conflictProps) =>
			await CoreApiConflictsRepo.GetConflict(this._config, conflictProps);

		[HttpPost]
		[Route("api/core/conflict/resolve/winner")]
		public async Task<object> ResolveWinner([FromBody] ConflictProperties conflictProps) =>
			await CoreApiConflictsRepo.ResolveConflict(this._config, conflictProps);

		[HttpPost]
		[Route("api/core/conflict/resolve/loser")]
		public async Task<object> ResolveLoser([FromBody] ConflictProperties conflictProps) =>
			await CoreApiConflictsRepo.ReverseConflict(this._config, conflictProps);

		#endregion

		#region "Indexing"

		[HttpGet]
		[Route("api/core/indexing/exclude")]
		public async Task<string> IndexingPathExclusions() =>
			await CoreApiIndexingRepo.IndexingPathExclusions(this._config);

		[HttpGet]
		[Route("api/core/indexing/composite")]
		public async Task<string> ManualIndexing() =>
			await CoreApiIndexingRepo.IndexingCompositePaths(this._config);

		[HttpGet]
		[Route("api/core/indexing/spatial")]
		public async Task<string> IndexingSpatialPaths() =>
			await CoreApiIndexingRepo.IndexingSpatialPaths(this._config);

        #endregion

        #region "Security"

        [HttpGet]
        [Route("api/core/users")]
        public async Task<IEnumerable<object>> GetUsers() =>
            await CoreApiSecurityRepo.GetUsers(this._config);

        [HttpGet]
        [Route("api/core/createUsers")]
        public async Task<string> CreateUsers() =>
            await CoreApiSecurityRepo.CreateUsers(this._config);

        [HttpGet]
        [Route("api/core/createPermissions")]
        public async Task<object> CreatePermissions() =>
            await CoreApiSecurityRepo.CreatePermissions(this._config);

        [HttpGet]
        [Route("api/core/permissions/{userId}")]
        public async Task<IEnumerable<object>> GetPermissions(string userId) =>
            await CoreApiSecurityRepo.GetPermissions(this._config, userId);

        [HttpGet]
        [Route("api/core/testPermissions/{userId}")]
        public async Task<string> TestPermissions(string userId) =>
            await CoreApiSecurityRepo.TestPermissions(this._config, userId);

        [HttpGet]
        [Route("api/core/deleteUsers")]
        public async Task<string> DeleteUsers() =>
            await CoreApiSecurityRepo.DeleteUsers(this._config);

        #endregion

        #region "Stored Procedures"

        [HttpGet]
        [Route("api/core/createSprocs")]
        public async Task<string> CreateStoredProcedures() =>
            await CoreApiServerRepo.CreateStoredProcedures(this._config);

        [HttpGet]
        [Route("api/core/sprocs")]
        public async Task<IEnumerable<object>> GetStoredProcedures() =>
            await CoreApiServerRepo.GetStoredProcedures(this._config);

        [HttpGet]
        [Route("api/core/executeSproc/{sprocId}")]
        public async Task<string> ExecuteStoredProcedures(string sprocId) =>
            await CoreApiServerRepo.ExecuteStoredProcedure(this._config, sprocId);

        [HttpGet]
        [Route("api/core/deleteSprocs")]
        public async Task<string> DeleteStoredProcedures() =>
            await CoreApiServerRepo.DeleteStoredProcedures(this._config);

        #endregion

        #region "Triggers"

        [HttpGet]
        [Route("api/core/createTriggers")]
        public async Task<string> CreateTriggers() =>
			await CoreApiServerRepo.CreateTriggers(this._config);

        [HttpGet]
        [Route("api/core/triggers")]
        public async Task<IEnumerable<object>> GetTriggers() =>
            await CoreApiServerRepo.GetTriggers(this._config);

        [HttpGet]
        [Route("api/core/executeTrigger/{triggerId}")]
        public async Task<string> ExecuteTrigger(string triggerId) =>
            await CoreApiServerRepo.ExecuteTrigger(this._config, triggerId);

		[HttpGet]
		[Route("api/core/deleteTriggers")]
		public async Task<string> DeleteTriggers() =>
			await CoreApiServerRepo.DeleteTriggers(this._config);

        #endregion

        #region "User-Defined Functions"

        [HttpGet]
        [Route("api/core/createUdfs")]
        public async Task<string> CreateUserDefinedFunctions() =>
            await CoreApiServerRepo.CreateUserDefinedFunctions(this._config);

        [HttpGet]
        [Route("api/core/udfs")]
        public async Task<IEnumerable<object>> GetUserDefinedFunctions() =>
            await CoreApiServerRepo.GetUserDefinedFunctions(this._config);

        [HttpGet]
        [Route("api/core/executeUdf/{udfId}")]
        public async Task<string> ExecuteUserDefinedFunction(string udfId) =>
            await CoreApiServerRepo.ExecuteUserDefinedFunction(this._config, udfId);

        [HttpGet]
        [Route("api/core/deleteUdfs")]
        public async Task<string> DeleteUserDefinedFunctions() =>
            await CoreApiServerRepo.DeleteUserDefinedFunctions(this._config);

        #endregion

    }
}
