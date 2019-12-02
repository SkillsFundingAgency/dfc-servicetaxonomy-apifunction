using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class JsonHelper : IJsonHelper
    {
        public dynamic DeserializeObject(string objectToDeserialize)
        {
            return JsonConvert.DeserializeObject(objectToDeserialize);
        }

        public T DeserializeObject<T>(string objectToDeserialize)
        {
            return JsonConvert.DeserializeObject<T>(objectToDeserialize);
        }
    }
}
