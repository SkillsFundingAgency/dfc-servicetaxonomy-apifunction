using DFC.ServiceTaxonomy.ApiFunction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface IQueryBuilder
    {
        string Build(QueryParameters queryParameters);
    }
}
