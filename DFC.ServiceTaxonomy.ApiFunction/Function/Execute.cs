using DFC.ServiceTaxonomy.ApiFunction.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Neo4j.Driver.V1;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptions<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IJsonHelper _jsonHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;

        public Execute(IOptions<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, IHttpRequestHelper httpRequestHelper, IJsonHelper jsonHelper, INeo4JHelper neo4JHelper, IFileHelper fileHelper)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? 
                                          throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            _httpRequestHelper = httpRequestHelper ??
                                 throw new ArgumentNullException(nameof(httpRequestHelper));

            _jsonHelper = jsonHelper ??
                  throw new ArgumentNullException(nameof(jsonHelper));

            _neo4JHelper = neo4JHelper ?? 
                           throw new ArgumentNullException(nameof(neo4JHelper));

            _fileHelper = fileHelper ??
                          throw new ArgumentNullException(nameof(fileHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var functionToProcess = _serviceTaxonomyApiSettings.Value.Function;

            if (string.IsNullOrWhiteSpace(functionToProcess))
            {
                log.LogInformation("Missing App Settings");
                return new InternalServerErrorResult();
            }

            log.LogInformation($"{functionToProcess} HTTP trigger function is processing a request.");

            log.LogInformation("Attempting to read body from http request");

            string requestBody = null;

            try
            {
                requestBody = await _httpRequestHelper.GetBodyFromHttpRequest(req);
            }
            catch (IOException ex)
            {
                log.LogError("Unable to read body from req", ex);
                return new BadRequestObjectResult("Unable to read body from req");
            }

            log.LogInformation("Attempting to Deserialize request body");

            dynamic jsonBody;

            try
            {
                jsonBody = JsonConvert.DeserializeObject(requestBody);
            }
            catch (JsonException ex)
            {
                log.LogError("Unable to retrieve body from req", ex);
                return new UnprocessableEntityObjectResult("Unable to deserialize request body");
            }

            log.LogInformation("generating file name and dir to read json config");

            var queryFileNameAndDir = $@"CypherQueries\{functionToProcess}.json";

            string cypherQueryJsonConfig = null;

            log.LogInformation("Attempting to read json config");

            try
            {
                cypherQueryJsonConfig = await _fileHelper.ReadAllTextFromFile(queryFileNameAndDir);
            }
            catch (Exception ex)
            {
                log.LogError($"Unable to read {queryFileNameAndDir} query file", ex);
                return new InternalServerErrorResult();
            }
            
            log.LogInformation("Attempting to Deserialize json config to cypher model");

            Cypher cypherModel;

            try
            {
                cypherModel = _jsonHelper.DeserializeObject<Cypher>(cypherQueryJsonConfig);
            }
            catch (JsonException ex)
            {
                log.LogError($"Unable to Deserialize json from {queryFileNameAndDir}", ex);
                return new InternalServerErrorResult();
            }

            if (cypherModel == null)
                return new InternalServerErrorResult();

            var cypherQuery = cypherModel.Query;

            var cypherQueryStatementParameters = new Dictionary<string, object>();

            log.LogInformation("Attempting to read json body object");

            if (jsonBody != null)
            {
                foreach (var json in jsonBody)
                {
                    var queryParam = cypherModel.QueryParam.FirstOrDefault(x => x.Name.Contains(json.Name));

                    if (queryParam != null)
                        cypherQueryStatementParameters.Add(json.Name, json.Value.ToString());
                }

                if (cypherModel.QueryParam.Count != cypherQueryStatementParameters.Count)
                    return new BadRequestObjectResult($"Expected {cypherModel.QueryParam.Count} query parameters, only {cypherQueryStatementParameters.Count} have been provided in the request body");
            }

            log.LogInformation("Attempting to query neo4j");

            IStatementResultCursor statementResultAsync;

            try
            {
                statementResultAsync = await _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(cypherQuery, cypherQueryStatementParameters);
            }
            catch (Exception ex)
            {
                log.LogError($"Unable To Run Query", ex);
                return new InternalServerErrorResult();
            }

            var listOfResults = await _neo4JHelper.GetListOfRecordsAsync(statementResultAsync);

            if (listOfResults == null)
                return new NoContentResult();

            var statementResult = await _neo4JHelper.GetResultSummaryAsync(statementResultAsync);

            if (statementResult != null)
                log.LogInformation(
                    $"Query: {statementResult.Statement.Text} \n Results Available After: {statementResult.ResultAvailableAfter}");

            log.LogInformation("request has successfully been completed with results");

            return new OkObjectResult(JsonConvert.SerializeObject(listOfResults));
          
        }
    }
}