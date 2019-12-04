using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using DFC.ServiceTaxonomy.ApiFunction.Function;
using DFC.ServiceTaxonomy.ApiFunction.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Options;
using Neo4j.Driver.V1;
using Newtonsoft.Json;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Tests
{
    public class ExecuteHttpTriggerTests
    {
        private readonly Execute _executeFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly ExecutionContext _executionContext;
        private readonly IOptions<ServiceTaxonomyApiSettings> _config;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IJsonHelper _jsonHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;
        private readonly Cypher _cypherModel;

        public ExecuteHttpTriggerTests()
        {
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _executionContext =  new ExecutionContext();
            var options = new ServiceTaxonomyApiSettings
            {
                Function = "GetAllSkills",
                Neo4jUrl = "bolt://localhost:11002",
                Neo4jUser = "NeoUser",
                Neo4jPassword = "NeoPass"
            };
            _config = Options.Create(options);
            _log = A.Fake<ILogger>();
            _httpRequestHelper = A.Fake<IHttpRequestHelper>();
            _jsonHelper = A.Fake<IJsonHelper>();
            _neo4JHelper = A.Fake<INeo4JHelper>();
            _fileHelper = A.Fake<IFileHelper>();
            _cypherModel = new Cypher {Query = "query", QueryParam = new List<QueryParam>{ new QueryParam { Name = "occupation" } } };

            _executeFunction = new Execute(_config, _httpRequestHelper, _jsonHelper, _neo4JHelper, _fileHelper);

        }

        [Fact]
        public async Task Execute_WhenFunctionAppSettingIsNullOrEmpty_ReturnsBadRequestObjectResult()
        {
            _config.Value.Function = null;

            var result = await RunFunction();

            var internalServerErrorResult = result as InternalServerErrorResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is InternalServerErrorResult);
            Assert.Equal((int?)HttpStatusCode.InternalServerError, internalServerErrorResult.StatusCode);
        }

        [Fact]
        public async Task Execute_WhenUnableToReadRequestBody_ReturnsBadRequestObjectResult()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Throws<IOException>();
            
            var result = await RunFunction();
            
            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is BadRequestObjectResult);
            Assert.Equal((int?) HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_WhenUnableToDeserializeRequestBody_ReturnsUnprocessableEntityObjectResult()
        {
            _config.Value.Function = "GetAllSkills";
            
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"test: test1");


            A.CallTo(() => _jsonHelper.DeserializeObject(A.Dummy<string>())).Throws<JsonException>();

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is UnprocessableEntityObjectResult);
            Assert.Equal((int?)HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_WhenJsonConfigQueryFileHasInvalidJson_ReturnsInternalServerErrorResult()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

             A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(A.Dummy<string>())).Throws<JsonException>();

            var result = await RunFunction();

            var internalServerErrorResult = result as InternalServerErrorResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is InternalServerErrorResult);
            Assert.Equal((int?)HttpStatusCode.InternalServerError, internalServerErrorResult.StatusCode);
        }
        
        [Fact]
        public async Task Execute_WhenCypherQueryIsEmpty_ReturnsInternalServerErrorResult()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetAllSkills.json")).Returns("{}");
           
            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>("{}")).Returns(null);

            var result = await RunFunction();

            var internalServerErrorResult = result as InternalServerErrorResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is InternalServerErrorResult);
            Assert.Equal((int?)HttpStatusCode.InternalServerError, internalServerErrorResult.StatusCode);
        }
        
        [Fact]
        public async Task Execute_WhenRequestBodyDoesntContainFieldsForCypherQuery_ReturnsBadRequestErrorMessageResult()
        {
            _config.Value.Function = "GetAllSkills";
            var query = "{\"query\": \"QUERY HERE\", \"queryParam\": [{\"name\": \"occupation\"}]}";
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetAllSkills.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var result = await RunFunction();

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is BadRequestObjectResult);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }
        
        [Fact]
        public async Task Execute_WhenCodeIsValid_ReturnsOkObjectResult()
        {
            _config.Value.Function = "GetAllSkills";
            var query = "{\"query\": \"QUERY HERE\", \"queryParam\": [{\"name\": \"occupation\"}]}";
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetAllSkills.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var dict = new Dictionary<string, object>
            {
                {"occupation", "http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"}
            };

            var statementResult = A.Fake<IStatementResultCursor>();
            var resultSummary = A.Fake<IResultSummary>();
            var records = new List<object> {new object()};

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync("query", dict)).Returns(statementResult);
            A.CallTo(() => _neo4JHelper.GetListOfRecordsAsync(statementResult)).Returns(records);
            A.CallTo(() => _neo4JHelper.GetResultSummaryAsync(statementResult)).Returns(resultSummary);
            
            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is OkObjectResult);
            Assert.Equal((int?)HttpStatusCode.OK, okObjectResult.StatusCode);
        }

        private async Task<IActionResult> RunFunction()
        {
            return await _executeFunction.Run(_request, _log, _executionContext).ConfigureAwait(false);
        }
    }
}