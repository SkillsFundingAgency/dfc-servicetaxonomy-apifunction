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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;

//todo: update to func v3, core 3.1, c# 8
//todo: nullable reference types
//todo: sonar

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;

        public Execute(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, IHttpRequestHelper httpRequestHelper, INeo4JHelper neo4JHelper, IFileHelper fileHelper)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));
            _httpRequestHelper = httpRequestHelper ?? throw new ArgumentNullException(nameof(httpRequestHelper));
            _neo4JHelper = neo4JHelper ?? throw new ArgumentNullException(nameof(neo4JHelper));
            _fileHelper = fileHelper ?? throw new ArgumentNullException(nameof(fileHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            try
            {
                log.LogInformation("HTTP trigger function is processing a request");

                Task<Cypher> cypherModelTask = GetCypherQuery(GetFunctionName(), context, log);
                Task<JObject> requestBodyTask = GetRequestBody(req, log);

                Cypher cypherModel = await cypherModelTask;
                var cypherQueryParameters = GetCypherQueryParameters(cypherModel, req.Query, await requestBodyTask, log);

                object recordsResult = await ExecuteCypherQuery(cypherModel, cypherQueryParameters, log);

                if (recordsResult == null)
                    return new NoContentResult();

                log.LogInformation("request has successfully been completed with results");

                var statementResult = await _neo4JHelper.GetResultSummaryAsync();
                if (statementResult != null)
                    log.LogInformation($"Query: {statementResult.Query.Text}\nResults available after: {statementResult.ResultAvailableAfter}");

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

        private string GetFunctionName()
        {
            string functionToProcess = _serviceTaxonomyApiSettings.CurrentValue.Function;

            if (string.IsNullOrWhiteSpace(functionToProcess))
                throw ApiFunctionException.InternalServerError("Missing function name in settings");

            return functionToProcess;
        }

        private async Task<object> ExecuteCypherQuery(Cypher cypherModel,
            Dictionary<string, object> cypherQueryParameters, ILogger log)
        {
            log.LogInformation($"Attempting to query neo4j with the following query: {cypherModel.Query}");

            try
            {
                return await _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(cypherModel.Query, cypherQueryParameters);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable To run query", ex);
            }
        }

        private async Task<Cypher> GetCypherQuery(string functionName, ExecutionContext context, ILogger log)
        {
            log.LogInformation("Generating file name and dir to read json config");

            //todo: fix so that auto works locally
            //var queryFileNameAndDir = $@"{context.FunctionAppDirectory}\CypherQueries\{functionName}.json";
            var queryFileNameAndDir = $@"{context.FunctionDirectory}\CypherQueries\{functionName}.json";

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
                cypherModel = JsonConvert.DeserializeObject<Cypher>(cypherQueryJsonConfig);
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to deserialize query file", ex);
            }

            if (cypherModel?.Query == null)
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

            log.LogInformation("Attempting to deserialize request body");

            JObject requestBody;

            if (string.IsNullOrWhiteSpace(requestBodyString))
                requestBodyString = "{}";

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

        private static object GetParamValue(string paramString, string paramType)
        {
            if (paramString == null)
            {
                return null;
            }
            switch (paramType)
            {
                case "System.String":
                    return paramString;
                case "System.Int32":
                    if (!Int32.TryParse(paramString, out int returnValue))
                    {
                        throw ApiFunctionException.BadRequest($"Unable to convert supplied Parameter to integer");
                    }
                    return returnValue;
                case "System.Array`1[System.String]":
                    return paramString.Split(",");
                default:
                    throw ApiFunctionException.InternalServerError($"Parameter type {paramType} not recognised");
            }
        }


        private static Dictionary<string, object> GetCypherQueryParameters(Cypher cypherModel, IQueryCollection queryCollection, JObject requestBody, ILogger log)
        {
            log.LogInformation("Attempting to read json body object");

            var cypherQueryStatementParameters = new Dictionary<string, object>();

            if (cypherModel.QueryParams == null)
                return cypherQueryStatementParameters;

            var queryParams = queryCollection.ToDictionary(p => p.Key, p => p.Value.Last(), StringComparer.OrdinalIgnoreCase);

            foreach (var cypherParam in cypherModel.QueryParams)
            {
                // let query param override param in message body

                try
                {
                    object foundParamValue = GetParamValue(queryParams.GetValueOrDefault(cypherParam.Name), cypherParam.Type)
                                             ?? requestBody.GetValue(cypherParam.Name, StringComparison.OrdinalIgnoreCase)?.ToObject(Type.GetType(cypherParam.Type ?? "System.String"))
                                             ?? cypherParam.Default;
                    if (foundParamValue == null)
                        throw ApiFunctionException.BadRequest($"Required parameter {cypherParam.Name} not found in request body or query params");

                    cypherQueryStatementParameters.Add(cypherParam.Name, foundParamValue);
                }
                catch (Exception ex)
                {
                    throw ApiFunctionException.InternalServerError("Unable to process supplied parameters", ex);
                }
            }

            return cypherQueryStatementParameters;
        }
    }
}
