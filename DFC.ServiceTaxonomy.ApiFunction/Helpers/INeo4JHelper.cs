﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface INeo4JHelper
    {
        Task<IStatementResultCursor> ExecuteCypherQueryInNeo4JAsync(string query,
            Dictionary<string, object> statementParameters);

        Task<IEnumerable<object>> GetListOfRecordsAsync(IStatementResultCursor statementResultCursor);

        Task<IResultSummary> GetResultSummaryAsync(IStatementResultCursor statementResultCursor);
    }
}