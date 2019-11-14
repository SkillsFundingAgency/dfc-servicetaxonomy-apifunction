using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class Cypher
    {
        public string Query { get; set; }
        public List<QueryParam> QueryParam = new List<QueryParam>();
    }
    
}
