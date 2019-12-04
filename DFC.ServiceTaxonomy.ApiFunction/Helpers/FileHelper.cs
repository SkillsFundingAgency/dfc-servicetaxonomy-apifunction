using System.IO;
using System.Threading.Tasks;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class FileHelper : IFileHelper
    {
        public async Task<string> ReadAllTextFromFileAsync(string fileName)
        {
            return await File.ReadAllTextAsync(fileName);
        }
    }
}
