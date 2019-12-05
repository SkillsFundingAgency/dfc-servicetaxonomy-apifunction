using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface IHttpRequestHelper
    {
        Task<string> GetBodyFromHttpRequestAsync(HttpRequest httpRequest);
    }
}
