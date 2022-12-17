using Newtonsoft.Json;

namespace Driver.Rpc.Response;


public struct RpcError
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}