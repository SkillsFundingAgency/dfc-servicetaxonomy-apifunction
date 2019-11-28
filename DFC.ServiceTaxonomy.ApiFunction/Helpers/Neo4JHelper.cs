using System;
using System.Collections.Generic;
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

        public IStatementResult ExecuteCypherQueryInNeo4J(string query, Dictionary<string, object> statementParameters)
        {
            var neo4JDriver = GetNeo4JDriver();

            using (var session = neo4JDriver.Session())
            {
                return session.Run(query, statementParameters);
            }

        }

        private IDriver GetNeo4JDriver()
        {
            return _neo4JDriver ??
                   (_neo4JDriver = GraphDatabase.Driver(_serviceTaxonomyApiSettings.Value.Neo4jUrl, _authToken));
        }

    }
}
