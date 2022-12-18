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

    public List<string> Queries { get; set; } = new();

    public DbQueryRequest(List<string> query, Dictionary<string, object?>? vars)
    {
        Id      = RpcId.GetRandom(6);
        Method  = "query";
        Queries = query;
        Params  = new List<object?> {GetQuery(), vars};
    }

    public string GetQuery()
    {
        return String.Join(Environment.NewLine, Queries);
    }
}