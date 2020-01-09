using DFC.ServiceTaxonomy.ApiFunction.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DFC.ServiceTaxonomy.ApiFunction.Exceptions;
using Newtonsoft.Json.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IJsonHelper _jsonHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;

        public Execute(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, IHttpRequestHelper httpRequestHelper, IJsonHelper jsonHelper, INeo4JHelper neo4JHelper, IFileHelper fileHelper)
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
            ILogger log, ExecutionContext context)
        {
            try
            {
                string functionToProcess = _serviceTaxonomyApiSettings.CurrentValue.Function;

                if (string.IsNullOrWhiteSpace(functionToProcess))
                {
                    log.LogInformation("Missing App Settings");
                    return new InternalServerErrorResult();
                }

                log.LogInformation($"{functionToProcess} HTTP trigger function is processing a request.");

                JObject requestBody = await GetRequestBody(req, log);

                var cypherModel = await GetCypherQuery(functionToProcess, context, log);

                var cypherQueryStatementParameters = GetCypherQueryParameters(cypherModel, req.Query, requestBody, log);

                await ExecuteCypherQuery(cypherModel, cypherQueryStatementParameters, log);

                var recordsResult = await _neo4JHelper.GetListOfRecordsAsync();

                if (recordsResult == null)
                    return new NoContentResult();

                var statementResult = await _neo4JHelper.GetResultSummaryAsync();

                if (statementResult != null)
                    log.LogInformation(
                        $"Query: {statementResult.Statement.Text} \n Results Available After: {statementResult.ResultAvailableAfter}");

                log.LogInformation("request has successfully been completed with results");

                return new OkObjectResult(recordsResult);
            }
            catch (ApiFunctionException e)
            {
                log.LogError(e.ToString());
                return e.ActionResult;
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new InternalServerErrorResult();
            }
        }

        private async Task ExecuteCypherQuery(Cypher cypherModel,
            Dictionary<string, object> cypherQueryStatementParameters, ILogger log)
        {
            //todo: return the cursor?
            log.LogInformation($"Attempting to query neo4j with the following query: {cypherModel.Query}");

            try
            {
                await _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(cypherModel.Query,
                    cypherQueryStatementParameters);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable To run query", ex);
            }
        }

        private async Task<Cypher> GetCypherQuery(string functionToProcess, ExecutionContext context, ILogger log)
        {
            log.LogInformation("Generating file name and dir to read json config");

            //var queryFileNameAndDir = $@"{context.FunctionAppDirectory}\CypherQueries\{functionToProcess}.json";
            var queryFileNameAndDir = $@"{context.FunctionDirectory}\CypherQueries\{functionToProcess}.json";

            string cypherQueryJsonConfig;

            log.LogInformation($"Attempting to read json config from {queryFileNameAndDir}");

            try
            {
                cypherQueryJsonConfig = await _fileHelper.ReadAllTextFromFileAsync(queryFileNameAndDir);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to read query file", ex);
            }

            log.LogInformation("Attempting to deserialize json config to cypher model");

            Cypher cypherModel;

            try
            {
                cypherModel = _jsonHelper.DeserializeObject<Cypher>(cypherQueryJsonConfig);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to deserialize query file", ex);
            }

            if (cypherModel == null)
                throw ApiFunctionException.InternalServerError("Null deserialized cypher model");
            
            return cypherModel;
        }

        private async Task<JObject> GetRequestBody(HttpRequest req, ILogger log)
        {
            log.LogInformation("Attempting to read body from http request");

            string requestBodyString;
            try
            {
                requestBodyString = await _httpRequestHelper.GetBodyFromHttpRequestAsync(req);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.BadRequest("Unable to read body from request", ex);
            }

            log.LogInformation("Attempting to Deserialize request body");

            JObject requestBody;

            try
            {
                requestBody = JObject.Parse(requestBodyString);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.UnprocessableEntityObjectResult("Unable to deserialize request body", ex);
            }

            return requestBody;
        }

        private static Dictionary<string, object> GetCypherQueryParameters(Cypher cypherModel, IQueryCollection queryCollection, JObject requestBody, ILogger log)
        {
            log.LogInformation("Attempting to read json body object");

            var cypherQueryStatementParameters = new Dictionary<string, object>();
            
            if (cypherModel.QueryParams == null)
                return cypherQueryStatementParameters;

            var queryParams = queryCollection.ToDictionary(p => p.Key, p => p.Value.Last(), StringComparer.OrdinalIgnoreCase);
            
            //todo: rename everything to be descriptive
            foreach (var cypherParam in cypherModel.QueryParams)
            {
                // let query param override param in message body
                string foundParamValue = queryParams.GetValueOrDefault(cypherParam.Name)
                                         ?? requestBody.GetValue(cypherParam.Name, StringComparison.OrdinalIgnoreCase)?.ToString()
                                         ?? cypherParam.Default;

                if (foundParamValue == null)
                    throw ApiFunctionException.BadRequest($"Required parameter {cypherParam.Name} not found in request body or query params");

                cypherQueryStatementParameters.Add(cypherParam.Name, foundParamValue);
            }

            return cypherQueryStatementParameters;
        }
    }
}

//todo:
// tests
// refactor
// non-nullable
// update to v3 func 3.1 core
// sonar?