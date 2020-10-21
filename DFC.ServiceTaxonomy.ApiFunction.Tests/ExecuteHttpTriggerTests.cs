using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Microsoft.Extensions.Primitives;
using Neo4j.Driver;
using Newtonsoft.Json;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Tests
{
    public class ExecuteHttpTriggerTests
    {
        private const string DefaultFunctionName = "GetAllSkills";
        private const string DefaultApiVersion = "V1";
        private readonly Execute _executeFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly ExecutionContext _executionContext;
        private readonly IOptionsMonitor<ServiceTaxonomyApiSettings> _config;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;
        private readonly IResultSummary _resultSummary;

        public ExecuteHttpTriggerTests()
        {
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _request.Headers.Add("X-Forwarded-Host", "test.com");

            _executionContext =  new ExecutionContext();

            _config = A.Fake<IOptionsMonitor<ServiceTaxonomyApiSettings>>();
            A.CallTo(() => _config.CurrentValue).Returns(new ServiceTaxonomyApiSettings
            {
                Function = DefaultFunctionName,
                Neo4jUrl = "bolt://localhost:11002",
                Neo4jUser = "NeoUser",
                Neo4jPassword = "NeoPass",
                Scheme = "https://",
                ApplicationName = "ServiceTaxonomy"
            });

            _log = A.Fake<ILogger>();
            _httpRequestHelper = A.Fake<IHttpRequestHelper>();

            _neo4JHelper = A.Fake<INeo4JHelper>();
            _fileHelper = A.Fake<IFileHelper>();
            _resultSummary = A.Fake<IResultSummary>();

            A.CallTo(() => _neo4JHelper.GetResultSummaryAsync()).Returns(_resultSummary);

            const string  query = "{\"query\": \"QUERY HERE\"}";
            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns(query);

            _executeFunction = new Execute(_config, _httpRequestHelper, _neo4JHelper, _fileHelper);
        }

        [Fact]
        public async Task Execute_WhenFunctionAppSettingIsNullOrEmpty_ReturnsInternalServerErrorResult()
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
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns("}bad json");

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
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns("bad json");
            
            var result = await RunFunction();

            var internalServerErrorResult = result as InternalServerErrorResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is InternalServerErrorResult);
            Assert.Equal((int?)HttpStatusCode.InternalServerError, internalServerErrorResult.StatusCode);
        }
        
        [Theory]
        [InlineData("")]      // deserialize returns null
        [InlineData("{}")]    // deserializes to CypherModel containing nulls
        public async Task Execute_WhenCypherQueryIsEmpty_ReturnsInternalServerErrorResult(string cypherConfig)
        {
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(@"{ ""occupation"": ""http://data.europa.eu/esco/occupation/5793c124-c037-47b2-85b6-dd4a705968dc"" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns(cypherConfig);
           
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
            var query = "{\"query\": \"QUERY HERE\", \"queryParams\": [{\"name\": \"occupation\"}]}";

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns(query);

            var result = await RunFunction();

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is BadRequestObjectResult);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_CypherConfigFound_CorrectQueryTextPassedToQuery()
        {
            const string expectedQueryText = "query text";

            var query = $"{{\"query\": \"{expectedQueryText}\"}}";

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns(query);

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(new object());

            await RunFunction();

            // Assert
            
            // could use the Capture from here.. https://thorarin.net/blog/post/2014/09/18/capturing-method-arguments-on-your-fakes-using-fakeiteasy.aspx
            var neo4JHelperCalls = Fake.GetCalls(_neo4JHelper).ToList();

            var executeCalls = neo4JHelperCalls.Where(c => c.Method.Name == nameof(INeo4JHelper.ExecuteCypherQueryInNeo4JAsync));
            Assert.Single(executeCalls);

            var executeCall = executeCalls.First();
            //todo: refactor unfriendly
            var actualQueryText = executeCall.Arguments[0].As<string>();
            Assert.Equal(expectedQueryText, actualQueryText);
        }
        
        [Theory]
        [InlineData("body", null, "default", "body")]
        [InlineData(null, "param", "default", "param")]
        [InlineData("body", "param", "default", "param")]
        [InlineData(null, null, "default", "default")]
        [InlineData("body", null, null, "body")]
        [InlineData(null, "param", null, "param")]
        [InlineData("body", "param", null, "param")]
        // see Execute_NoParamSuppliedForMandatoryParam_ReturnsBadRequest() for this scenario..
        //[InlineData(null, null, null, <return BadRequest>)]
        public async Task Execute_ParamMatrix_CorrectParamValuePassedToQuery(
            string requestBodyParam, string queryParamParam, string defaultParam, string expectedCypherParam)
        {
            const string paramName = "paramName";

            string requestBody = requestBodyParam != null
                ? $@"{{""{paramName}"": ""{requestBodyParam}"" }}"
                : "{}";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request))
                .Returns(requestBody);
            
            if (queryParamParam != null)
            {
                _request.Query = new QueryCollection(new Dictionary<string, StringValues>
                {
                    {paramName, new StringValues(queryParamParam)}
                });
            }

            string cypherConfig = $@"{{
                ""query"": """",
                ""queryParams"": [
                    {{
                        ""name"": ""{paramName}""
                        {(defaultParam != null ? $", \"default\": \"{defaultParam}\"" : "")}
                    }}
                ]}}";

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns(cypherConfig);

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(new object());

            var result = await RunFunction();

            // Assert
            Assert.True(result is OkObjectResult);
            
            // could use the Capture from here.. https://thorarin.net/blog/post/2014/09/18/capturing-method-arguments-on-your-fakes-using-fakeiteasy.aspx
            var neo4JHelperCalls = Fake.GetCalls(_neo4JHelper).ToList();

            var executeCalls = neo4JHelperCalls.Where(c => c.Method.Name == nameof(INeo4JHelper.ExecuteCypherQueryInNeo4JAsync));
            Assert.Single(executeCalls);

            var executeCall = executeCalls.First();
            //todo: refactor unfriendly
            var actualParams = executeCall.Arguments[1].As<IDictionary<string, object>>();
            Assert.Equal(expectedCypherParam, actualParams[paramName]);
        }

        [Fact]
        public async Task Execute_NoParamSuppliedForMandatoryParam_ReturnsBadRequest()
        {
            const string paramName = "paramName";

            string cypherConfig = $@"{{
                ""query"": """",
                ""queryParams"": [
                    {{
                        ""name"": ""{paramName}""
                    }}
                ]}}";

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\{DefaultFunctionName}.json")).Returns(cypherConfig);

            var dictionaryOfRecords = new Dictionary<string, object> { { "occupations", new object[0] } };

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(dictionaryOfRecords);

            var result = await RunFunction();

            Assert.True(result is BadRequestObjectResult);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  \n")]
        [InlineData("{}")]
        public async Task Execute_GetAllSkills_ReturnsCorrectJsonResponse(string requestBody)
        {
            var expectedJson = @"{""skills"":[{""uri"":""http://data.europa.eu/esco/skill/68698869-c13c-4563-adc7-118b7644f45d"",""skill"":""identify customer's needs"",""skillType"":""knowledge"",""alternativeLabels"":[""alt 1"",""alt 2"",""alt 3""],""jobProfile"":""http://tbc""}]}";
            
            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns(requestBody);

            var record = new Dictionary<string, object>
            {
                {"uri", "http://data.europa.eu/esco/skill/68698869-c13c-4563-adc7-118b7644f45d"},
                {"skill", "identify customer's needs"},
                {"skillType", "knowledge"},
                {"alternativeLabels", new[] {"alt 1", "alt 2", "alt 3"}},
                {"jobProfile", "http://tbc"}
            };

            object dictionaryOfRecords = new Dictionary<string, object> { { "skills", new object[] { record } } };

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(dictionaryOfRecords);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }
        
        [Fact]
        public async Task Execute_GetAllOccupations_ReturnsCorrectJsonResponse()
        {
             _config.CurrentValue.Function = "GetAllOccupations";
            var expectedJson = @"{""occupations"":[{""uri"":""http://data.europa.eu/esco/occupation/114e1eff-215e-47df-8e10-45a5b72f8197"",""occupation"":""renewable energy consultant"",""alternativeLabels"":[""alt 1"",""alt 2"",""alt 3""],""lastModified"":""05-12-2019T00:00:00Z""}]}";
            var query = @"{""query"": ""QUERY HERE""}";

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\GetAllOccupations.json")).Returns(query);

            var record = new Dictionary<string, object>
            {
                {"uri", "http://data.europa.eu/esco/occupation/114e1eff-215e-47df-8e10-45a5b72f8197"},
                {"occupation", "renewable energy consultant"},
                {"alternativeLabels", new[] {"alt 1", "alt 2", "alt 3"}},
                {"lastModified", "05-12-2019T00:00:00Z"}
            };

            object dictionaryOfRecords = new Dictionary<string, object> { { "occupations", new object[] { record } } };

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(dictionaryOfRecords);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }

        [Fact]
        public async Task Execute_GetOccupationsByLabel_ReturnsCorrectJsonResponse()
        {
            _config.CurrentValue.Function = "GetOccupationsByLabel";
            var expectedJson = "{\"occupations\":[{\"uri\":\"http://data.europa.eu/esco/occupation/c95121e9-e9f7-40a9-adcb-6fda1e82bbd2\",\"occupation\":\"hazardous waste technician\",\"alternativeLabels\":[\"waste disposal site compliance technician\",\"toxic waste removal technician\"],\"lastModified\":\"03-12-2019T00:00:00Z\",\"matches\":{\"occupation\":[],\"alternativeLabels\":[\"toxic waste removal technician\"]}}]}";
            var query = @"{""query"": ""QUERY HERE""}";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns("{\"label\": \"toxic\" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\GetOccupationsByLabel.json")).Returns(query);

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

            object dictionaryOfRecords = new Dictionary<string, object> { { "occupations", new object[] { record } } };

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(dictionaryOfRecords);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }

        [Fact]
        public async Task Execute_PathParameter_ReturnsExecutedCallWithQueryParameter()
        {
            _config.CurrentValue.Function = "GetJobProfileByTitle";
            var query = @"{""query"": ""QUERY HERE"",""queryParams"": [{""name"": ""canonicalName"",""pathOrdinalPosition"": 0}]}";
            _request.Path = "/Execute/Librarian";

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\GetJobProfileByTitle.json")).Returns(query);
            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(Task.CompletedTask);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            var neo4JHelperCalls = Fake.GetCalls(_neo4JHelper).ToList();

            var executeCalls = neo4JHelperCalls.Where(c => c.Method.Name == nameof(INeo4JHelper.ExecuteCypherQueryInNeo4JAsync));

            var executeCall = executeCalls.First();

            var actualParams = executeCall.Arguments[1].As<IDictionary<string, object>>();

            //Assert
            Assert.True(result is OkObjectResult);
            Assert.Single(executeCalls);
            Assert.Equal("Librarian", actualParams["canonicalName"]);
        }

        [Fact]
        public async Task Execute_GetSkillsByLabel_ReturnsCorrectJsonResponse()
        {
            _config.CurrentValue.Function = "GetSkillsByLabel";
            var expectedJson = "{\"skills\":[{\"uri\":\"http://data.europa.eu/esco/skill/b70ab677-5781-40b5-9198-d98f4a34310f\",\"skill\":\"toxicology\",\"skillType\":\"knowledge\",\"skillReusability\":\"cross-sectoral\",\"alternativeLabels\":[\"study of toxicity\",\"chemical toxicity\",\"study of adverse effects of chemicals\",\"studies of toxicity\"],\"lastModified\":\"2016-12-20T19:32:45Z\",\"matches\":{\"skill\":[\"toxicology\"],\"alternativeLabels\":[\"study of toxicity\",\"chemical toxicity\",\"studies of toxicity\"],\"hiddenLabels\":[]}}]}";
            var query = @"{""query"": ""QUERY HERE""}";

            A.CallTo(() => _httpRequestHelper.GetBodyFromHttpRequestAsync(_request)).Returns("{\"label\": \"toxic\" }");

            A.CallTo(() => _fileHelper.ReadAllTextFromFileAsync($"\\CypherQueries\\{DefaultApiVersion}\\GetSkillsByLabel.json")).Returns(query);

            var record = new Dictionary<string, object>
            {
                {"uri", "http://data.europa.eu/esco/skill/b70ab677-5781-40b5-9198-d98f4a34310f"},
                {"skill", "toxicology"},
                {"skillType", "knowledge"},
                {"skillReusability", "cross-sectoral"},
                {"alternativeLabels", new [] {"study of toxicity","chemical toxicity","study of adverse effects of chemicals","studies of toxicity"}},
                {"lastModified", "2016-12-20T19:32:45Z"},
                {
                    "matches", new Dictionary<string, object>
                    {
                        {"skill", new[] {"toxicology"}},
                        {"alternativeLabels", new[] {"study of toxicity","chemical toxicity","studies of toxicity"}},
                        {"hiddenLabels", new string[0]},
                    }
                }
            };

            object dictionaryOfRecords = new Dictionary<string, object> { { "skills", new object[] { record } } };

            A.CallTo(() => _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(A<string>.Ignored, A<IDictionary<string, object>>.Ignored)).Returns(dictionaryOfRecords);

            var result = await RunFunction();

            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            var balzak = JsonConvert.SerializeObject(okObjectResult.Value);
            Assert.Equal(expectedJson, JsonConvert.SerializeObject(okObjectResult.Value));
        }
        
        private async Task<IActionResult> RunFunction()
        {
            return await _executeFunction.Run(_request, _log, _executionContext).ConfigureAwait(false);
        }
    }
}