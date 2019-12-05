namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public interface IJsonHelper
    {
        dynamic DeserializeObject(string objectToDeserialize);
        T DeserializeObject<T>(string objectToDeserialize);
    }
}
