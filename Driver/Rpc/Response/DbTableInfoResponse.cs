using Driver.Json;
using Newtonsoft.Json;

namespace Driver.Rpc.Response;

public class DbTableInfoResponse
{
    [JsonProperty("ev")]
    public Dictionary<string, string> Events = new();

    [JsonProperty("ft")]
    public Dictionary<string, string> ft = new();

    [JsonProperty("fd")]
    public Dictionary<string, string> Fields = new();

    [JsonProperty("ix")]
    public Dictionary<string, string> Indexes = new();

    public static DbTableInfoResponse? FromJson(string json) => DbJson.Deserialize<DbTableInfoResponse>(json);
}