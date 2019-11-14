using System.Collections.Generic;
using Neo4j.Driver.V1;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface INeo4JHelper
    {
        IStatementResult ExecuteCypherQueryInNeo4J(string query, Dictionary<string, object> statementParameters);
    }
}