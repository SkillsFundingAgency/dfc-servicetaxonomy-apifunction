using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver.V1;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4JHelper : INeo4JHelper, IDisposable
    {
        private readonly IAuthToken _authToken = AuthTokens.None;
        private readonly IDriver _neo4JDriver;
        private IStatementResultCursor _statementResultCursor;

        public Neo4JHelper(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, ILogger log)
        {
            var taxonomyApiSettings = serviceTaxonomyApiSettings?.CurrentValue ?? 
                                        throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            if (string.IsNullOrEmpty(taxonomyApiSettings.Neo4jUrl))
                throw new Exception("Missing Neo4j database uri setting.");
            
            if (string.IsNullOrEmpty(taxonomyApiSettings.Neo4jUser) ||
                string.IsNullOrEmpty(taxonomyApiSettings.Neo4jPassword))
            {
                log.LogWarning("No credentials for Neo4j database in settings, attempting connection without authorization token.");
            }
            else
            {
                _authToken = AuthTokens.Basic(taxonomyApiSettings.Neo4jUser, taxonomyApiSettings.Neo4jPassword);
            }

            _neo4JDriver = GraphDatabase.Driver(taxonomyApiSettings.Neo4jUrl, _authToken);
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

            var neoRecords = records.SelectMany(x => x.Values.Values);

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
