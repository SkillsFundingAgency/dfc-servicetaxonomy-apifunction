using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Options;
using Neo4j.Driver.V1;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4JHelper : INeo4JHelper, IDisposable
    {
        private readonly IAuthToken _authToken = AuthTokens.None;
        private readonly IDriver _neo4JDriver;
        private IStatementResultCursor _statementResultCursor;

        public Neo4JHelper(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings)
        {
            var taxonomyApiSettings = serviceTaxonomyApiSettings ?? 
                                                          throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            if (!string.IsNullOrEmpty(taxonomyApiSettings.CurrentValue.Neo4jUser) && 
                !string.IsNullOrEmpty(taxonomyApiSettings.CurrentValue.Neo4jPassword))
                _authToken = AuthTokens.Basic(taxonomyApiSettings.CurrentValue.Neo4jUser, taxonomyApiSettings.CurrentValue.Neo4jPassword);

            _neo4JDriver = GraphDatabase.Driver(taxonomyApiSettings.CurrentValue.Neo4jUrl, _authToken);

        }
        
        public async Task ExecuteCypherQueryInNeo4JAsync(string query, Dictionary<string, object> statementParameters)
        {
           
            using (var session = _neo4JDriver.Session())
            {
                try
                {
                    _statementResultCursor =
                        await session.ReadTransactionAsync(async tx => await tx.RunAsync(query, statementParameters));
                }
                finally
                {
                    await session.CloseAsync();
                }
            }

        }

        public async Task<object> GetListOfRecordsAsync()
        {
            var records =  await _statementResultCursor.ToListAsync();

            if (records == null || !records.Any())
                return null;

            var neoRecords = records.SelectMany(x => x.Values.Values).ToList();

            return neoRecords.FirstOrDefault();
        }

        public async Task<IResultSummary> GetResultSummaryAsync()
        {
            return await _statementResultCursor.SummaryAsync();
        }

        public void Dispose()
        {
            _neo4JDriver?.Dispose();
        }

    }
}
