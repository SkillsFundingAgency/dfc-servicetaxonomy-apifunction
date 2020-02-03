using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface INeo4JHelper
    {
        Task<object> ExecuteCypherQueryInNeo4JAsync(string query,
            IDictionary<string, object> statementParameters, ILogger log);

        Task<IResultSummary> GetResultSummaryAsync();
    }
}