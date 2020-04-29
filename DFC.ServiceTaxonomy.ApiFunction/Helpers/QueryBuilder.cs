using DFC.ServiceTaxonomy.ApiFunction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class QueryBuilder : IQueryBuilder
    {
        public string Build(QueryParameters queryParameters)
        {
            if (string.IsNullOrWhiteSpace(queryParameters.Id))
            {
                return $"MATCH (n:ncs__{queryParameters.ContentType}) return n;";
            }

            return $"MATCH (n:ncs__{queryParameters.Id} {{ name: 'Oliver Stone' }})";
        }
    }
}
