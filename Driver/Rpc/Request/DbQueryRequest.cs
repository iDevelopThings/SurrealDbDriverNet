using Driver.Utils;
using Newtonsoft.Json;

namespace Driver.Rpc.Request;

public struct DbQueryRequest : IRpcRequest
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("params")]
    public List<object?>? Params { get; set; }

    public DbQueryRequest(string query, Dictionary<string, object?>? vars)
    {
        Id     = RpcId.GetRandom(6);
        Method = "query";
        Params = new List<object?> {query, vars};
    }
}