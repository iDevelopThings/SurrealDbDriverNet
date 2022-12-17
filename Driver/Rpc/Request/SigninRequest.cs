using Newtonsoft.Json;

namespace Driver.Rpc.Request;

public struct SigninRequest
{
    [JsonProperty("user")]
    public string Username { get; set; }

    [JsonProperty("pass")]
    public string Password { get; set; }
}