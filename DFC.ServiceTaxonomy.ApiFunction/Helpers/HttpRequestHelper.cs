using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class HttpRequestHelper : IHttpRequestHelper
    {
        public async Task<string> GetBodyFromHttpRequest(HttpRequest httpRequest)
        {
            using (var sr = new StreamReader(httpRequest.Body))
            {
               return await sr.ReadToEndAsync();
            }
        }

    }
}
