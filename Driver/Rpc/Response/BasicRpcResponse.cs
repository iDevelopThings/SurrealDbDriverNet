using Driver.Json;
using Driver.Rpc.Request;
using Newtonsoft.Json;

namespace Driver.Rpc.Response;

public struct BasicRpcResponse : IRpcResponse
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("error")]
    public RpcError? Error { get; set; }

    [JsonProperty("result")]
    public object? Result { get; set; }

    public IRpcRequest Request { get; set; }

    public string RawJson { get; set; }

    public bool IsError()
    {
        return Error != null && !string.IsNullOrEmpty(Error.Value.Message);
    }

    public void FinalErrorHandling()
    {
        if (!IsError()) return;

        Console.WriteLine("Db RPC Error Response: \n" + DbJson.Serialize(this, Formatting.Indented));
    }
}