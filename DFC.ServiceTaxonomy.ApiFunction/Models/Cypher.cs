using System.Collections.Generic;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class Cypher
    {
        public string Query { get; set; }
        public List<QueryParam> QueryParam { get; set; }
    }
    
}
