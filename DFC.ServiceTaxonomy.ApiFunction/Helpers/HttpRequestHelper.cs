using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class HttpRequestHelper : IHttpRequestHelper
    {
        public async Task<string> GetBodyFromHttpRequestAsync(HttpRequest httpRequest)
        {
            return await httpRequest.ReadAsStringAsync();
        }
    }
}
