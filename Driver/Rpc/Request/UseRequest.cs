using Newtonsoft.Json;

namespace Driver.Rpc.Request;

public struct UseRequest
{
    [JsonProperty("ns")]
    public string Namespace { get; set; }

    [JsonProperty("db")]
    public string Database { get; set; }
}