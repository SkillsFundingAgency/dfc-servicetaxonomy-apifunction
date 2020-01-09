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

                log.LogInformation("Attempting to read body from http request");

                string requestBodyString;

                try
                {
                    requestBodyString = await _httpRequestHelper.GetBodyFromHttpRequestAsync(req);
                }
                catch (Exception ex)
                {
                    log.LogError("Unable to read body from req", ex);
                    return new BadRequestObjectResult("Unable to read body from req");
                }

                log.LogInformation("Attempting to Deserialize request body");

                JObject requestBody;

                try
                {
                    requestBody = JObject.Parse(requestBodyString);
                }
                catch (Exception ex)
                {
                    log.LogError("Unable to retrieve body from req", ex);
                    return new UnprocessableEntityObjectResult("Unable to deserialize request body");
                }

                log.LogInformation("generating file name and dir to read json config");

                //var queryFileNameAndDir = $@"{context.FunctionAppDirectory}\CypherQueries\{functionToProcess}.json";
                var queryFileNameAndDir = $@"{context.FunctionDirectory}\CypherQueries\{functionToProcess}.json";

                string cypherQueryJsonConfig;

                log.LogInformation("Attempting to read json config");

                try
                {
                    cypherQueryJsonConfig = await _fileHelper.ReadAllTextFromFileAsync(queryFileNameAndDir);
                }
                catch (Exception ex)
                {
                    log.LogError($"Unable to read {queryFileNameAndDir} query file, \n Function Directory: {context.FunctionDirectory} \n Function App Directory: {context.FunctionAppDirectory}  \n Exception:" + ex, ex);
                    throw new Exception($"Unable to read {queryFileNameAndDir} query file", ex);
                }
                
                log.LogInformation("Attempting to Deserialize json config to cypher model");

                Cypher cypherModel;

                try
                {
                    cypherModel = _jsonHelper.DeserializeObject<Cypher>(cypherQueryJsonConfig);
                }
                catch (Exception ex)
                {
                    log.LogError($"Unable to Deserialize json from {queryFileNameAndDir}", ex);
                    return new InternalServerErrorResult();
                }

                if (cypherModel == null)
                    return new InternalServerErrorResult();

                log.LogInformation("Attempting to read json body object");

                var cypherQueryStatementParameters = GetCypherQueryParameters(req.Query, log, cypherModel, requestBody);

                log.LogInformation($"Attempting to query neo4j with the following query: {cypherModel.Query}");
                
                try
                {
                    await _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(cypherModel.Query, cypherQueryStatementParameters);
                }
                catch (Exception ex)
                {
                    log.LogError($"Unable To Run Query \n Exception:" + ex, ex);
                    throw new Exception("Unable To Run Query", ex);
                }

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
                log.LogError(e.Message);
                return e.ActionResult;
            }
        }

        private static Dictionary<string, object> GetCypherQueryParameters(IQueryCollection queryCollection, ILogger log, Cypher cypherModel, JObject requestBody)
        {
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