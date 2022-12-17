using Driver.Json;
using Newtonsoft.Json;

namespace Driver.Rpc.Response;

public class DbQueryResult<TDataType>
{
    [JsonProperty("result")]
    public List<TDataType>? Result { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("time")]
    public string? Time { get; set; }
}

public interface IRpcResponseExtended : IRpcResponse
{
    public bool HasMultipleQueryResults { get; }
    public bool IsSingleQueryResult     { get; }
    public bool IsSingleItem            { get; }
    public bool IsManyItem              { get; }
    public bool IsEmpty                 { get; }

    public void FinalErrorHandling();

    public object? Process();

    public TObjectType? Process<TObjectType>();
}

public class DbQueryResponse<TDataType> : IRpcResponseExtended
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("error")]
    public RpcError? Error { get; set; }

    [JsonProperty("result")]
    public object? Result { get; set; }

    public bool HasMultipleQueryResults { get; set; }
    public bool IsSingleQueryResult     { get; set; }
    public bool IsSingleItem            { get; set; }
    public bool IsManyItem              { get; set; }
    public bool IsEmpty                 { get; set; }

    public string? RawJson { get; set; }

    private bool _processedResult = false;

    public bool IsError()
    {
        return Error != null && !string.IsNullOrEmpty(Error.Value.Message);
    }

    public void FinalErrorHandling()
    {
        if (!IsError()) return;

        Console.WriteLine("Db RPC Error Response: \n" + DbJson.Serialize(this, Formatting.Indented));
    }

    public List<DbQueryResult<TDataType?>>? ParseResponse()
    {
        if (_processedResult) return Result as List<DbQueryResult<TDataType?>>;

        var resultObj = DbJson.Deserialize<List<DbQueryResult<TDataType?>>>(Result?.ToString()!);

        Result           = resultObj;
        _processedResult = true;


        IsSingleQueryResult     = resultObj.Count == 1;
        HasMultipleQueryResults = resultObj.Count > 1;
        IsSingleItem            = resultObj.Count == 1 && resultObj[0].Result?.Count == 1;
        IsManyItem              = resultObj.Count == 1 && resultObj[0].Result?.Count > 1;
        IsEmpty                 = resultObj.Count == 0 || resultObj[0].Result == null || resultObj[0].Result?.Count == 0;

        return Result as List<DbQueryResult<TDataType?>>;
    }

    public TDataType? First()
    {
        var data = ParseResponse();

        var qResult = data?.FirstOrDefault();
        if (qResult == null) return default;
        if (qResult.Result == null) return default;

        return qResult.Result.FirstOrDefault();
    }

    public List<TDataType?> Get()
    {
        var data = ParseResponse();
        if (data?.Count == 0 || data == null) return new();

        return data.SelectMany(x => x.Result!).ToList();
    }

    public List<List<TDataType?>?>? AllResponses()
    {
        var data = ParseResponse();

        return data?.Select(x => x.Result)?.ToList();
    }

    public object? Process() => Process<object?>();

    public TObjectType? Process<TObjectType>()
    {
        return DbJson.Deserialize<TObjectType>(Result?.ToString()!);
    }

    public Type GetResultType() => typeof(TDataType);
}