using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.ApiFunction.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver.V1;
using Newtonsoft.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class ExcuteCypher
    {
        private readonly IOptions<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly INeo4JHelper _neo4JHelper;

        public ExcuteCypher(IOptions<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, INeo4JHelper neo4JHelper)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? 
                                          throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));
            _neo4JHelper = neo4JHelper ?? 
                           throw new ArgumentNullException(nameof(neo4JHelper));
        }

        [FunctionName("ExcuteCypher")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var functionToProcess = _serviceTaxonomyApiSettings.Value.Function;

            if(string.IsNullOrWhiteSpace(functionToProcess))
                return new BadRequestObjectResult("Function cannot be found in app settings");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic jsonBody;

            try
            {
                jsonBody = JsonConvert.DeserializeObject(requestBody);
            }
            catch (JsonException e)
            {
                return new UnprocessableEntityObjectResult("Unable to deserialize request body");
            }

            var queryFileNameAndDir = string.Format(@"{0}\{1}.{2}", "CypherQueries" , functionToProcess, "json");

            var text = File.ReadAllText(queryFileNameAndDir);

            if(string.IsNullOrWhiteSpace(text))
                return new BadRequestObjectResult(string.Format("Unable to read {0} json config file", functionToProcess));

            var cypherModel = JsonConvert.DeserializeObject<Cypher>(text);

            if(cypherModel == null)
                return new UnprocessableEntityObjectResult(string.Format("Unable to deserialize {0} text file", functionToProcess));

            var cypherQuery = cypherModel.Query;

            if(string.IsNullOrWhiteSpace(cypherQuery))
                return new BadRequestObjectResult("Query cannot be empty");

            var cypherQueryStatementParameters = new Dictionary<string, object>();

            if (jsonBody != null)
            {
                foreach (var json in jsonBody)
                {
                    var queryParam = cypherModel.QueryParam.FirstOrDefault(x => x.Name.Contains(json.Name));

                    if (queryParam != null)
                        cypherQueryStatementParameters.Add(json.Name, json.Value.ToString());
                }

                if (cypherModel.QueryParam.Any() && !cypherQueryStatementParameters.Any())
                    return new UnprocessableEntityObjectResult("No Query Parameters have been provided in the request body");
            }
            
            var statementResult = _neo4JHelper.ExecuteCypherQueryInNeo4J(cypherQuery, cypherQueryStatementParameters);
            
            if(statementResult == null)
                return new NoContentResult();

            if (statementResult.Summary != null)
                log.LogInformation(string.Format("Query: {0} \n Results Available After: {1}", 
                    statementResult.Summary.Statement.Text, 
                    statementResult.Summary.ResultAvailableAfter));

            return new OkObjectResult(JsonConvert.SerializeObject(statementResult.SelectMany(x => x.Values.Values)));
          
        }
    }

}