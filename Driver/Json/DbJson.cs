using Newtonsoft.Json;


namespace Driver.Json;

public class DbJson
{
    public static JsonSerializerSettings SerializeSettings => new JsonSerializerSettings() {
        ContractResolver = new DatabaseJsonConverterContract()
    };

    public static JsonSerializerSettings DeserializeSettings => new JsonSerializerSettings() {
        ContractResolver = new DatabaseJsonConverterContract()
    };

    public static JsonSerializer Create() => JsonSerializer.Create(SerializeSettings);

    public static string Serialize(object obj, Formatting formatting = Formatting.None)
        => JsonConvert.SerializeObject(obj, formatting, SerializeSettings);

    public static T Deserialize<T>(string json)
        => JsonConvert.DeserializeObject<T>(json, DeserializeSettings)!;

    public static object? Deserialize(string json, Type type)
        => JsonConvert.DeserializeObject(json, type, DeserializeSettings);

    public static void Populate<T>(string json, T obj)
    {
        JsonConvert.PopulateObject(json, obj!, DeserializeSettings);
    }
}