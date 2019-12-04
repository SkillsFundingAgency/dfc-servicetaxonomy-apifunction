using System.Threading.Tasks;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface IFileHelper
    {
        Task<string> ReadAllTextFromFileAsync(string fileName);
    }
}
