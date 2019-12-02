using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface IFileHelper
    {
        string ReadAllTextFromFile(string fileName);
    }
}
