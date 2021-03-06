﻿namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ServiceTaxonomyApiSettings
    {
        public string Function { get; set; }
        public string Neo4jUrl { get; set; }
        public string Neo4jUser { get; set; }
        public string Neo4jPassword { get; set; }
        public string Scheme { get; set; }
        public string ApplicationName { get; set; }
        public string WebsiteHost { get; set; }
    }
}
