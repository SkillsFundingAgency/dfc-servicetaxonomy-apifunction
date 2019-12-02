using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class FileHelper : IFileHelper
    {
        public string ReadAllTextFromFile(string fileName)
        {
            return File.ReadAllText(fileName);
        }
    }
}
