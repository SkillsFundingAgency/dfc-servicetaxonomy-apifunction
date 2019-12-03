using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface IFileHelper
    {
        Task<string> ReadAllTextFromFile(string fileName);
    }
}
