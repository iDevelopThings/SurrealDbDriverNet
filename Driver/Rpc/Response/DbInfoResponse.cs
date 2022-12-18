using Driver.Json;
using Newtonsoft.Json;

namespace Driver.Rpc.Response;

public class DbInfoResponse
{
    [JsonProperty("dl")]
    public Dictionary<string, string> dl = new();

    [JsonProperty("dt")]
    public Dictionary<string, string> dt = new();

    [JsonProperty("sc")]
    public Dictionary<string, string> Scopes = new();

    [JsonProperty("tb")]
    public Dictionary<string, string> Tables = new();

    public static DbInfoResponse? FromJson(string json) => DbJson.Deserialize<DbInfoResponse>(json);
}