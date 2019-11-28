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
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptions<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly INeo4JHelper _neo4JHelper;

        public Execute(IOptions<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, IHttpRequestHelper httpRequestHelper, INeo4JHelper neo4JHelper)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? 
                                          throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            _httpRequestHelper = httpRequestHelper ??
                                 throw new ArgumentNullException(nameof(httpRequestHelper));

            _neo4JHelper = neo4JHelper ?? 
                           throw new ArgumentNullException(nameof(neo4JHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var functionToProcess = _serviceTaxonomyApiSettings.Value.Function;

            if(string.IsNullOrWhiteSpace(functionToProcess))
                return new BadRequestObjectResult("Function cannot be found in app settings");

            log.LogInformation(string.Format("{0} HTTP trigger function processed a request.", functionToProcess));

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

            var queryFileNameAndDir = string.Format(@"{0}\{1}.{2}", "CypherQueries" , functionToProcess, "json");

            string cypherQueryJsonConfig = string.Empty;

            log.LogInformation("Attempting to read json config");

            try
            {
                cypherQueryJsonConfig = File.ReadAllText(queryFileNameAndDir);
            }
            catch (Exception ex)
            {
                log.LogError(string.Format("Unable to read {0} query file", queryFileNameAndDir), ex);
                return new UnprocessableEntityObjectResult(string.Format("Unable to read {0} query file", queryFileNameAndDir));
            }

            if (string.IsNullOrWhiteSpace(cypherQueryJsonConfig))
                return new UnprocessableEntityObjectResult(string.Format("No content in {0} query file", functionToProcess));

            log.LogInformation("Attempting to Deserialize json config to cypher model");

            Cypher cypherModel;

            try
            {
                cypherModel = JsonConvert.DeserializeObject<Cypher>(cypherQueryJsonConfig);
            }
            catch (JsonException ex)
            {
                log.LogError(string.Format("Unable to Deserialize json from {0}", queryFileNameAndDir), ex);
                return new UnprocessableEntityObjectResult(string.Format("Unable to Deserialize json from {0}", queryFileNameAndDir));
            }

            if (cypherModel == null)
                return new UnprocessableEntityObjectResult(string.Format("Unable to deserialize {0} text file", functionToProcess));

            var cypherQuery = cypherModel.Query;

            if(string.IsNullOrWhiteSpace(cypherQuery))
                return new UnprocessableEntityObjectResult("Query cannot be empty");

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

                if (cypherModel.QueryParam.Any() && !cypherQueryStatementParameters.Any())
                    return new BadRequestErrorMessageResult("No Query Parameters have been provided in the request body");
            }

            log.LogInformation("Attempting to query neo4j");

            var statementResult = _neo4JHelper.ExecuteCypherQueryInNeo4J(cypherQuery, cypherQueryStatementParameters);
            
            if(statementResult == null)
                return new NoContentResult();

            if (statementResult.Summary != null)
                log.LogInformation(string.Format("Query: {0} \n Results Available After: {1}", 
                    statementResult.Summary.Statement.Text, 
                    statementResult.Summary.ResultAvailableAfter));

            log.LogInformation("request has successfully been completed with results");

            return new OkObjectResult(JsonConvert.SerializeObject(statementResult.SelectMany(x => x.Values.Values)));
          
        }
    }

}