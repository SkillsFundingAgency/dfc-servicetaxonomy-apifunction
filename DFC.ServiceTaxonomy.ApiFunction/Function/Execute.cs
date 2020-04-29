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
using Neo4j.Driver;

//todo: update to func v3, core 3.1, c# 8
//todo: nullable reference types
//todo: sonar

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly IOptionsMonitor<ContentTypeMapSettings> _contentTypeMapSettings;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly INeo4JHelper _neo4JHelper;
        private readonly IFileHelper _fileHelper;
        private readonly IQueryBuilder _queryBuilder;
        private const string typeString = "System.String";
        private const string typeInt = "System.Int32";
        private const string typeStringArray = "System.String[]";

        private static string contentByIdCypher = "MATCH (n {uri:{0}}) return n;";
        private static string contentGetAllCypher = "MATCH (n:{}) return n;";

        public Execute(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, IOptionsMonitor<ContentTypeMapSettings> contentTypeMapSettings, IHttpRequestHelper httpRequestHelper, INeo4JHelper neo4JHelper, IFileHelper fileHelper, IQueryBuilder queryBuilder)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));
            _contentTypeMapSettings = contentTypeMapSettings ?? throw new ArgumentNullException(nameof(contentTypeMapSettings));
            _httpRequestHelper = httpRequestHelper ?? throw new ArgumentNullException(nameof(httpRequestHelper));
            _neo4JHelper = neo4JHelper ?? throw new ArgumentNullException(nameof(neo4JHelper));
            _fileHelper = fileHelper ?? throw new ArgumentNullException(nameof(fileHelper));
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Execute/{contentType}/{identifier}")] HttpRequest req, string contentType, Guid? id,
            ILogger log, ExecutionContext context)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
                log.LogInformation($"Function has been triggered in {environment} environment.");

                bool development = environment == "Development";

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    throw ApiFunctionException.BadRequest($"Required parameter contentType not found in path.");
                }

                var queryParameters = new QueryParameters { ContentType = contentType, Id = id };

                object recordsResult = await ExecuteCypherQuery(queryParameters, log);

                if (recordsResult == null)
                    return new NoContentResult();

                log.LogInformation("request has successfully been completed with results");

                //var statementResult = await _neo4JHelper.GetResultSummaryAsync();
                //if (statementResult != null)
                //    log.LogInformation($"Query: {statementResult.Query.Text}\nResults available after: {statementResult.ResultAvailableAfter}");

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

        private async Task<object> ExecuteCypherQuery(QueryParameters queryParameters, ILogger log)
        {
            log.LogInformation($"Attempting to query neo4j with the following query: Content Type: {queryParameters.ContentType} Identifier: {queryParameters.Id}");

            try
            {
                //Could move in to helper class
                var queryToExecute = this.BuildQuery(queryParameters);
                return await _neo4JHelper.ExecuteCypherQueryInNeo4JAsync();
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable To run query", ex);
            }
        }

        private string BuildQuery(QueryParameters queryParameters)
        {
            if (!queryParameters.Id.HasValue)
            {
                //GetAll Query
                return string.Format(contentGetAllCypher, MapContentTypeToNamespace(queryParameters.ContentType));
            }
            else
            {

            }
        }

        private string MapContentTypeToNamespace(string contentType)
        {
            _contentTypeMapSettings.CurrentValue.Values.TryGetValue(contentType, out string mappedValue);

            if (string.IsNullOrWhiteSpace(mappedValue))
            {
                throw ApiFunctionException.BadRequest($"Content Type {contentType} is not mapped in AppSettings");
            }

            return mappedValue;
        }

        private async Task<Cypher> GetCypherQuery(string functionName, ExecutionContext context, bool development, ILogger log)
        {
            log.LogInformation("Generating file name and dir to read json config");

            var queryFileNameAndDir = $@"{(development ? context.FunctionAppDirectory : context.FunctionDirectory)}\CypherQueries\{functionName}.json";

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
            switch (paramType ?? typeString)
            {
                case typeString:
                    return paramString;
                case typeInt:
                    if (!Int32.TryParse(paramString, out int returnValue))
                    {
                        throw ApiFunctionException.BadRequest($"Unable to convert supplied Parameter to integer");
                    }
                    return returnValue;
                case typeStringArray:
                    return paramString.Split(",");
                default:
                    throw ApiFunctionException.InternalServerError($"Parameter type {paramType} not recognised");
            }
        }


        private static Dictionary<string, object> GetCypherQueryParameters(Cypher cypherModel, IQueryCollection queryCollection, JObject requestBody, ILogger log, Dictionary<string, object> existingParameters)
        {
            log.LogInformation("Attempting to read json body object");

            if (cypherModel.QueryParams == null)
                return existingParameters;

            var queryParams = queryCollection.ToDictionary(p => p.Key, p => p.Value.Last(), StringComparer.OrdinalIgnoreCase);

            foreach (var cypherParam in cypherModel.QueryParams)
            {
                // let query param override param in message body
                if (!existingParameters.ContainsKey(cypherParam.Name))
                {
                    try
                    {
                        object foundParamValue = GetParamValue(queryParams.GetValueOrDefault(cypherParam.Name), cypherParam.Type)
                                                 ?? requestBody.GetValue(cypherParam.Name, StringComparison.OrdinalIgnoreCase)?.ToObject(Type.GetType(cypherParam.Type ?? typeString))
                                                 ?? cypherParam.Default;
                        if (foundParamValue == null)
                            throw ApiFunctionException.BadRequest($"Required parameter {cypherParam.Name} not found in request body or query params");

                        existingParameters.Add(cypherParam.Name, foundParamValue);
                    }
                    catch (Exception ex)
                    {
                        throw ApiFunctionException.BadRequest("Unable to process supplied parameters", ex);
                    }
                }
            }

            return existingParameters;
        }
    }
}
