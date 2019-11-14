using System;
using System.Collections.Generic;
using System.Text;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Options;
using Neo4j.Driver.V1;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4JHelper : INeo4JHelper
    {

        private readonly IOptions<ServiceTaxonomyApiSettings> _serviceTaxonomyApiSettings;
        private IAuthToken authToken = AuthTokens.None;
        private IDriver _neo4jDriver;

        public Neo4JHelper(IOptions<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings)
        {
            _serviceTaxonomyApiSettings = serviceTaxonomyApiSettings ?? 
                                          throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            if (!string.IsNullOrEmpty(_serviceTaxonomyApiSettings.Value.Neo4jUser) && !string.IsNullOrEmpty(_serviceTaxonomyApiSettings.Value.Neo4jPassword))
                authToken = AuthTokens.Basic(_serviceTaxonomyApiSettings.Value.Neo4jUser, _serviceTaxonomyApiSettings.Value.Neo4jPassword);
        }

        public IStatementResult ExecuteCypherQueryInNeo4J(string query, Dictionary<string, object> statementParameters)
        {
            var neo4JDriver = GetNeo4jDriver();

            using (var session = neo4JDriver.Session())
            {
                return session.Run(query, statementParameters);
            }

        }

        private IDriver GetNeo4jDriver()
        {
            return _neo4jDriver ??
                   (_neo4jDriver = GraphDatabase.Driver(_serviceTaxonomyApiSettings.Value.Neo4jUrl, authToken));
        }

    }
}
