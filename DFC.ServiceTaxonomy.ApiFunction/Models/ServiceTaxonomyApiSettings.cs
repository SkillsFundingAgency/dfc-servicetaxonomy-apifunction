namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ServiceTaxonomyApiSettings
    {
        public string Function { get { return "GetJobProfileByTitle"; } }
        public string Neo4jUrl { get { return "bolt://localhost:7687"; } }
        public string Neo4jUser { get { return "neo4j"; } }
        public string Neo4jPassword { get { return "ESCO3"; } }
    }
}
