using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class FileHelper : IFileHelper
    {
        public async Task<string> ReadAllTextFromFile(string fileName)
        {
            return await File.ReadAllTextAsync(fileName);
        }
    }
}
