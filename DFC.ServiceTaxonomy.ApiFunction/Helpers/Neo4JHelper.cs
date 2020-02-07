using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4JHelper : INeo4JHelper, IDisposable
    {
        private readonly IAuthToken _authToken = AuthTokens.None;
        private IDriver _neo4JDriver = null;
        private IResultCursor _resultCursor;
        private String _neo4JUrl;
        //private DriverLogger _log4j;
        //private Neo4jLoggingHelper _log4j;

        public Neo4JHelper(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings)
        {
            var taxonomyApiSettings = serviceTaxonomyApiSettings?.CurrentValue ?? 
                                        throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            if (string.IsNullOrEmpty(taxonomyApiSettings.Neo4jUrl))
                throw new Exception("Missing Neo4j database uri setting.");
            
            if (string.IsNullOrEmpty(taxonomyApiSettings.Neo4jUser) ||
                string.IsNullOrEmpty(taxonomyApiSettings.Neo4jPassword))
            {
                throw new Exception("No credentials for Neo4j database in settings, attempting connection without authorization token.");
            }
            else
            {
                _authToken = AuthTokens.Basic(taxonomyApiSettings.Neo4jUser, taxonomyApiSettings.Neo4jPassword);
            }
            _neo4JUrl = taxonomyApiSettings.Neo4jUrl;
        }

        public IDriver CreateDriver(string uri, IAuthToken authToken, Neo4jLoggingHelper log)
        {
            return GraphDatabase.Driver(uri, authToken,
                o => o.WithMaxConnectionLifetime(TimeSpan.FromMinutes(30))
                    .WithMaxConnectionPoolSize(50)
                    .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(2))
                    .WithLogger(log)
                    .WithFetchSize(Config.Infinite));
        }


        //todo: create package for DFC.ServiceTaxonomy.Neo4j??
        public async Task<object> ExecuteCypherQueryInNeo4JAsync(string query, IDictionary<string, object> statementParameters, Neo4jLoggingHelper log)
        {
            if (_neo4JDriver == null)
            {
               // _log4j = new DriverLogger(log);
                log.Info("Making initial bolt connection");
                _neo4JDriver = CreateDriver(_neo4JUrl, _authToken, log);
            }

            
            Object result = null;
           
            for (int i = 0; i < 5; i++)
            {
                IAsyncSession session = _neo4JDriver.AsyncSession();// o => o.WithDefaultAccessMode(AccessMode.Read)
                                                                    //    .WithFetchSize(Config.Infinite));
                bool fail = false;
                try
                {
                    result = await session.ReadTransactionAsync(async tx =>
                    {
                        _resultCursor = await tx.RunAsync(query, statementParameters);
                        var records = await GetListOfRecordsAsync();
                        var summary = await _resultCursor.ConsumeAsync();
                        log.resultsReadyAfter = (long)summary.ResultAvailableAfter.TotalMilliseconds;
                        log.resultsConsumedAfter = (long)summary.ResultConsumedAfter.TotalMilliseconds;
                        log.resultsRetries = i;
                        return records;
                    });
                }
                catch (Exception e)
                {
                    log.Warn(e,"Failed at attempt {i} of 5: ");
                    fail = true;
                }
                finally
                {
                    await session.CloseAsync();
                }
                if (!fail)
                {
                    break;
                }
            }
            return result;
        }

        private async Task<object> GetListOfRecordsAsync()
        {
            var records =  await _resultCursor.ToListAsync();

            if (records == null || !records.Any())
                return null;

            var neoRecords = records.SelectMany(x => x.Values.Values);

            return neoRecords.FirstOrDefault();
        }

        /// <summary>
        /// Calling this method will discard all remaining records to yield the summary
        /// </summary>
        public async Task<IResultSummary> GetResultSummaryAsync()
        {
            return await _resultCursor.ConsumeAsync();
        }

        public void Dispose()
        {
            _neo4JDriver?.Dispose();
        }
    }
}
