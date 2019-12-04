using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Options;
using Neo4j.Driver.V1;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4JHelper : INeo4JHelper
    {

        private readonly IOptions<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private readonly IAuthToken _authToken = AuthTokens.None;
        private IDriver _neo4JDriver;

        public Neo4JHelper(IOptions<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? 
                                          throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            if (!string.IsNullOrEmpty(_serviceTaxonomyApiSettings.Value.Neo4jUser) && !string.IsNullOrEmpty(_serviceTaxonomyApiSettings.Value.Neo4jPassword))
                _authToken = AuthTokens.Basic(_serviceTaxonomyApiSettings.Value.Neo4jUser, _serviceTaxonomyApiSettings.Value.Neo4jPassword);
        }
        
        public async Task<IStatementResultCursor> ExecuteCypherQueryInNeo4JAsync(string query, Dictionary<string, object> statementParameters)
        {
            var neo4JDriver = GetNeo4JDriver();

            using (var session = neo4JDriver.Session())
            {
                IStatementResultCursor statementResultCursor;

                try
                {
                    statementResultCursor =
                        await session.ReadTransactionAsync(async tx => await tx.RunAsync(query, statementParameters));
                }
                finally
                {
                    await session.CloseAsync();
                }

                return statementResultCursor;
            }

        }

        public async Task<IEnumerable<object>> GetListOfRecordsAsync(IStatementResultCursor statementResultCursor)
        {
            var records =  await statementResultCursor.ToListAsync();

            if (records == null || !records.Any())
                return null;

            return records.SelectMany(x => x.Values.Values);
        }

        public async Task<IResultSummary> GetResultSummaryAsync(IStatementResultCursor statementResultCursor)
        {
            return await statementResultCursor.SummaryAsync();
        }

        private IDriver GetNeo4JDriver()
        {
            return _neo4JDriver ??
                   (_neo4JDriver = GraphDatabase.Driver(_serviceTaxonomyApiSettings.Value.Neo4jUrl, _authToken));
        }

    }
}
