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
        private readonly IOptionsMonitor<ServiceTaxonomyApiSettings> _config;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IJsonHelper _jsonHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;
        private readonly Cypher _cypherModel;

        public ExecuteHttpTriggerTests()
        {
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _executionContext =  new ExecutionContext();

            _config = A.Fake<IOptionsMonitor<ServiceTaxonomyApiSettings>>();
            var serviceTaxonomyApiSettings = A.Fake<ServiceTaxonomyApiSettings>();
            serviceTaxonomyApiSettings.Function = "GetAllSkills";
            serviceTaxonomyApiSettings.Neo4jUrl = "bolt://localhost:11002";
            serviceTaxonomyApiSettings.Neo4jUser = "NeoUser";
            serviceTaxonomyApiSettings.Neo4jPassword = "NeoPass";
            A.CallTo(() => _config.CurrentValue).Returns(serviceTaxonomyApiSettings);

            _log = A.Fake<ILogger>();
            _httpRequestHelper = A.Fake<IHttpRequestHelper>();
            _jsonHelper = A.Fake<IJsonHelper>();
            _neo4JHelper = A.Fake<INeo4JHelper>();
            _fileHelper = A.Fake<IFileHelper>();
            _cypherModel = new Cypher {Query = "query", QueryParams = new List<QueryParam>() };

            _executeFunction = new Execute(_config, _httpRequestHelper, _jsonHelper, _neo4JHelper, _fileHelper);

        }

        [Fact]
        public async Task Execute_WhenFunctionAppSettingIsNullOrEmpty_ReturnsBadRequestObjectResult()
        {
             _config.CurrentValue.Function = null;

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
             _config.CurrentValue.Function = "GetAllSkills";

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
             _config.CurrentValue.Function = "GetAllSkills";
            
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
             _config.CurrentValue.Function = "GetAllSkills";

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
             _config.CurrentValue.Function = "GetAllSkills";

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
             _config.CurrentValue.Function = "GetAllSkills";
            var query = "{\"query\": \"QUERY HERE\", \"queryParam\": [{\"name\": \"occupation\"}]}";
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetAllSkills.json")).Returns(query);
            
            _cypherModel.QueryParams.Add(new QueryParam { Name = "occupation" });

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var result = await RunFunction();

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is BadRequestObjectResult);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }
        
        [Fact]
        public async Task Execute_WhenCodeIsValidForGetAllSkills_ReturnsCorrectJsonResponse()
        {
             _config.CurrentValue.Function = "GetAllSkills";
            var expectedJson = @"{""skills"":[{""uri"":""http://data.europa.eu/esco/skill/68698869-c13c-4563-adc7-118b7644f45d"",""skill"":""identify customer's needs"",""skillType"":""knowledge"",""alternativeLabels"":[""alt 1"",""alt 2"",""alt 3""],""jobProfile"":""http://tbc""}]}";
            
            var query = "{\"query\": \"QUERY HERE\"}";
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetAllSkills.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var dict = new Dictionary<string, object>();

            var resultSummary = A.Fake<IResultSummary>();
            var record = new Dictionary<string, object>
            {
                {"uri", "http://data.europa.eu/esco/skill/68698869-c13c-4563-adc7-118b7644f45d"},
                {"skill", "identify customer's needs"},
                {"skillType", "knowledge"},
                {"alternativeLabels", new string[3] {"alt 1", "alt 2", "alt 3"}},
                {"jobProfile", "http://tbc"}
            };

            var dictionaryOfRecords = new Dictionary<string, object> { { "skills", new object[1] { record } } };
            object records = dictionaryOfRecords;

            A.CallTo(() => _neo4JHelper.GetListOfRecordsAsync()).Returns(records);
            A.CallTo(() => _neo4JHelper.GetResultSummaryAsync()).Returns(resultSummary);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }
        
        [Fact]
        public async Task Execute_WhenCodeIsValidForGetAllOccupations_ReturnsCorrectJsonResponse()
        {
             _config.CurrentValue.Function = "GetAllOccupations";
            var expectedJson = @"{""occupations"":[{""uri"":""http://data.europa.eu/esco/occupation/114e1eff-215e-47df-8e10-45a5b72f8197"",""occupation"":""renewable energy consultant"",""alternativeLabels"":[""alt 1"",""alt 2"",""alt 3""],""lastModified"":""05-12-2019T00:00:00Z""}]}";
            var query = @"{""query"": ""QUERY HERE""}]}";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetAllOccupations.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var resultSummary = A.Fake<IResultSummary>();
            var record = new Dictionary<string, object>
            {
                {"uri", "http://data.europa.eu/esco/occupation/114e1eff-215e-47df-8e10-45a5b72f8197"},
                {"occupation", "renewable energy consultant"},
                {"alternativeLabels", new string[3] {"alt 1", "alt 2", "alt 3"}},
                {"lastModified", "05-12-2019T00:00:00Z"}
            };

            var dictionaryOfRecords = new Dictionary<string, object> { { "occupations", new object[1] { record } } };
            object records = dictionaryOfRecords;

            A.CallTo(() => _neo4JHelper.GetListOfRecordsAsync()).Returns(records);
            A.CallTo(() => _neo4JHelper.GetResultSummaryAsync()).Returns(resultSummary);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }

        [Fact]
        public async Task Execute_WhenCodeIsValidForGetOccupationsByLabel_ReturnsCorrectJsonResponse()
        {
            _config.CurrentValue.Function = "GetOccupationsForLabel";
            var expectedJson = "{\"occupations\":[{\"uri\":\"http://data.europa.eu/esco/occupation/c95121e9-e9f7-40a9-adcb-6fda1e82bbd2\",\"occupation\":\"hazardous waste technician\",\"alternativeLabels\":[\"waste disposal site compliance technician\",\"toxic waste removal technician\"],\"lastModified\":\"03-12-2019T00:00:00Z\",\"matches\":{\"occupation\":[],\"alternativeLabels\":[\"toxic waste removal technician\"]}}]}";
            var query = @"{""query"": ""QUERY HERE""}]}";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns("{\"label\": \"toxic\" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync("\\CypherQueries\\GetOccupationsByLabel.json")).Returns(query);

            A.CallTo(() => _jsonHelper.DeserializeObject<Cypher>(query)).Returns(_cypherModel);

            var resultSummary = A.Fake<IResultSummary>();
            var record = new Dictionary<string, object>
            {
                {"uri", "http://data.europa.eu/esco/occupation/c95121e9-e9f7-40a9-adcb-6fda1e82bbd2"},
                {"occupation", "hazardous waste technician"},
                {"alternativeLabels", new [] {"waste disposal site compliance technician", "toxic waste removal technician"}},
                {"lastModified", "03-12-2019T00:00:00Z"},
                {
                    "matches", new Dictionary<string, object>
                    {
                        {"occupation", new string[0]},
                        {"alternativeLabels", new[] {"toxic waste removal technician"}}
                    }
                }
            };

            var dictionaryOfRecords = new Dictionary<string, object> { { "occupations", new object[] { record } } };
            object records = dictionaryOfRecords;

            A.CallTo(() => _neo4JHelper.GetListOfRecordsAsync()).Returns(records);
            A.CallTo(() => _neo4JHelper.GetResultSummaryAsync()).Returns(resultSummary);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }

        private async Task<IActionResult> RunFunction()
        {
            return await _executeFunction.Run(_request, _log, _executionContext).ConfigureAwait(false);
        }
    }
}