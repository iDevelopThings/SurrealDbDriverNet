using Driver.Json;
using Driver.Rpc.Request;
using Newtonsoft.Json;

namespace Driver.Rpc.Response;

public interface IRpcResponse
{
    string?   Id     { get; set; }
    RpcError? Error  { get; set; }
    object?   Result { get; set; }

    IRpcRequest Request { get; set; }

    // Type         GetResultType();
    public string? RawJson { get; set; }

    bool IsError();
    // void         FinalErrorHandling();
    // object?      Process();
    // TObjectType? Process<TObjectType>();
}

public interface IRpcResponseCustom<T> : IRpcResponse
{
    new T? Result { get; set; }
}

public struct RpcResponse<TDataType> : IRpcResponse
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

    public TDataType? Get() => Process<TDataType>();

    public object? Process() => Process<TDataType>();

    public TObjectType? Process<TObjectType>()
    {
        return DbJson.Deserialize<TObjectType?>(Result?.ToString()!);
    }

    public Type GetResultType() => typeof(TDataType);
}