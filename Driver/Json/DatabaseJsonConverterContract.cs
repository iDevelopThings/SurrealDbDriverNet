using Newtonsoft.Json.Serialization;

namespace Driver.Json;

public class DatabaseJsonConverterContract : DefaultContractResolver
{
    public static readonly DatabaseJsonConverterContract Instance = new();

    protected override JsonContract CreateContract(Type objectType)
    {
        JsonContract contract = base.CreateContract(objectType);

        // this will only be called once and then cached
        // if (objectType == typeof(DateTime) || objectType == typeof(DateTimeOffset)) {
        // contract.Converter = new JavaScriptDateTimeConverter();
        // }

        // if (objectType == typeof(SdbPoint)) {
        // contract.Converter = new SdbPoint.Converter();
        // }

        return contract;
    }
}