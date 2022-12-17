using Driver.Utils;
using Newtonsoft.Json;

namespace Driver.Rpc.Request;

public interface IRpcRequest
{
    string         Id     { get; set; }
    string         Method { get; set; }
    List<object?>? Params { get; set; }
}

public struct RpcRequest : IRpcRequest
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("params")]
    public List<object?>? Params { get; set; }

    public RpcRequest(string method)
    {
        Id     = RpcId.GetRandom(6);
        Method = method;
        Params = new();
    }

    public RpcRequest(string method, List<object?>? @params)
    {
        Id     = RpcId.GetRandom(6);
        Method = method;
        Params = @params;
    }
}