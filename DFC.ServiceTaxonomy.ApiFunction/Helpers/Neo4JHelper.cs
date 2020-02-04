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
    public class DriverLogger : Neo4j.Driver.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _delegator;
        public DriverLogger(Microsoft.Extensions.Logging.ILogger delegator)
        {
            _delegator = delegator;
        }
        public void Error(Exception cause, string format, params object[] args)
        {
            _delegator.LogError(default(EventId), cause, format, args);
        }
        public void Warn(Exception cause, string format, params object[] args)
        {
            _delegator.LogWarning(default(EventId), cause, format, args);
        }
        public void Info(string format, params object[] args)
        {
            _delegator.LogInformation(default(EventId), format, args);
        }
        public void Debug(string format, params object[] args)
        {
            _delegator.LogDebug(default(EventId), format, args);
        }
        public void Trace(string format, params object[] args)
        {
            _delegator.LogTrace(default(EventId), format, args);
        }
        public bool IsTraceEnabled()
        {
            return _delegator.IsEnabled(LogLevel.Trace);
        }
        public bool IsDebugEnabled()
        {
            return _delegator.IsEnabled(LogLevel.Debug);
        }

    }

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
        
        //todo: create package for DFC.ServiceTaxonomy.Neo4j??
        public async Task<object> ExecuteCypherQueryInNeo4JAsync(string query, IDictionary<string, object> statementParameters, Neo4jLoggingHelper log)
        {
            if (_neo4JDriver == null)
            {
               // _log4j = new DriverLogger(log);
                log.Info("Making initial bolt connection");
                _neo4JDriver = GraphDatabase.Driver(_neo4JUrl, _authToken, o => o.WithLogger(log));
            }
            
            IAsyncSession session = _neo4JDriver.AsyncSession();
            try
            {
                return await session.ReadTransactionAsync(async tx =>
                {
                    _resultCursor = await tx.RunAsync(query, statementParameters);
                    return await GetListOfRecordsAsync();
                });
            }
            finally
            {
                await session.CloseAsync();
            }
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
