using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Castle.Components.DictionaryAdapter;
using DFC.ServiceTaxonomy.ApiFunction.Function;
using DFC.ServiceTaxonomy.ApiFunction.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
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
        private IOptions<ServiceTaxonomyApiSettings> _config;
        private IHttpRequestHelper _httpRequestHelper;
        private IJsonHelper _jsonHelper;
        private INeo4JHelper _neo4JHelper;
        private IFileHelper _fileHelper;
        private Cypher _blankCypherModel = null;
        private Cypher _cypherModel;

        public ExecuteHttpTriggerTests()
        {
            _request = new DefaultHttpRequest(new DefaultHttpContext());
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
        public async Task Execute_ReturnsBadRequestObjectResult_WhenFunctionAppSettingIsNullOrEmpty()
        {
            _config.Value.Function = null;

            var result = await RunFunction();

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_ReturnsBadRequestObjectResult_WhenUnableToReadRequestBody()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Throws<IOException>();
            
            var result = await RunFunction();
            
            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?) HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_ReturnsUnprocessableEntityObjectResult_WhenUnableToDeserializeRequestBody()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _jsonHelper.DeserializeObject(A.Dummy<string>())).Throws<JsonException>();

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }


        [Fact]
        public async Task Execute_ReturnsUnprocessableEntityObjectResult_WhenUnableToReadJsonConfigQueryFile()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFile("CypherQueries\\GetAllSkills.json")).Throws<Exception>();

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_ReturnsUnprocessableEntityObjectResult_WhenJsonConfigQueryFileIsNullOrEmpty()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFile("CypherQueries\\GetAllSkills.json")).Returns(string.Empty);

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }


        [Fact]
        public async Task Execute_ReturnsUnprocessableEntityObjectResult_WhenJsonConfigQueryFileHasInvalidJson()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

             A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(A.Dummy<string>())).Throws<JsonException>();

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_ReturnsUnprocessableEntityObjectResult_WhenJsonConfigQueryFileHasEmptyJson()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFile("CypherQueries\\GetAllSkills.json")).Returns(string.Empty);

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_ReturnsUnprocessableEntityObjectResult_WhenCypherQueryIsEmpty()
        {
            _config.Value.Function = "GetAllSkills";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFile("CypherQueries\\GetAllSkills.json")).Returns("{\"query\": \"QUERY HERE\", \"queryParam\": [{\"name\": \"Occupation\"}]}");
           
            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(A.Dummy<string>())).Returns(_blankCypherModel);

            var result = await RunFunction();

            var unprocessableEntityObjectResult = result as UnprocessableEntityObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?) HttpStatusCode.UnprocessableEntity, unprocessableEntityObjectResult.StatusCode);
        }
        
        [Fact]
        public async Task Execute_ReturnsBadRequestErrorMessageResult_WhenRequestBodyDoesntContainFieldsForCypherQuery()
        {
            _config.Value.Function = "GetAllSkills";
            var query = "{\"query\": \"QUERY HERE\", \"queryParam\": [{\"name\": \"occupation\"}]}";
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFile("CypherQueries\\GetAllSkills.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var result = await RunFunction();

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }
        
        [Fact]
        public async Task Execute_ReturnsNoContentResult_WhenNoResultsAreReturnedFromNeo4J()
        {
            _config.Value.Function = "GetAllSkills";
            var query = "{\"query\": \"QUERY HERE\", \"queryParam\": [{\"name\": \"occupation\"}]}";
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequest(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFile("CypherQueries\\GetAllSkills.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var dict = new Dictionary<string, object>
            {
                {"occupation", "http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"}
            };
            
            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4J("query", dict)).Returns((IStatementResult) null);

            var result = await RunFunction();

            var noContentResult = result as NoContentResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.Equal((int?)HttpStatusCode.NoContent, noContentResult.StatusCode);
        }

        private async Task<IActionResult> RunFunction()
        {
            return await _executeFunction.Run(_request, _log).ConfigureAwait(false);
        }
    }
}
