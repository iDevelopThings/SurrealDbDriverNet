using Driver.Json;
using Driver.Rpc.Request;
using Newtonsoft.Json;

namespace Driver.Rpc.Response;

public class ResultWrapper<TDataType>
{
    public object Value { get; set; } = null!;

    public bool IsObject { get; set; } = false;
    public bool IsArray  { get; set; } = false;

    public List<TDataType> EndResult { get; set; } = new List<TDataType>();

    public int Count => EndResult?.Count ?? 0;
}

public class DbQueryResult<TDataType>
{
    [JsonProperty("result")]
    public ResultWrapper<TDataType>? ResultWrapped { get; set; } = null!;

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("time")]
    public string? Time { get; set; }
}

public class ResultConverter<TDataType> : JsonConverter<ResultWrapper<TDataType>>
{
    public override ResultWrapper<TDataType> ReadJson(JsonReader reader, Type objectType, ResultWrapper<TDataType>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject) {
            // Deserialize the object as a single object
            var value = serializer.Deserialize<TDataType>(reader);
            return new ResultWrapper<TDataType> {
                IsObject  = true,
                Value     = value!,
                EndResult = new() {value!}
            };
        }

        if (reader.TokenType == JsonToken.StartArray) {
            // Deserialize the object as an array of objects
            var value = serializer.Deserialize<List<TDataType>>(reader);
            return new ResultWrapper<TDataType> {
                IsArray   = true,
                Value     = value!,
                EndResult = value!,
            };
        }

        if (reader.TokenType == JsonToken.Null) {
            return new ResultWrapper<TDataType> {
                IsArray   = true,
                Value     = null!,
                EndResult = null!,
            };
        }

        throw new JsonException("Unexpected token type: " + reader.TokenType);
    }

    public override void WriteJson(JsonWriter writer, ResultWrapper<TDataType>? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public interface IRpcResponseExtended : IRpcResponse
{
    public bool HasMultipleQueryResults { get; }
    public bool IsSingleQueryResult     { get; }
    public bool IsSingleItem            { get; }
    public bool IsManyItem              { get; }
    public bool IsEmpty                 { get; }

    IRpcRequest Request { get; set; }

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

    public string?     RawJson { get; set; }
    public IRpcRequest Request { get; set; } = null!;

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

        var resultObj = new List<DbQueryResult<TDataType?>>();
        DbJson.Populate<List<DbQueryResult<TDataType?>>>(Result?.ToString()!, resultObj);

        Result           = resultObj;
        _processedResult = true;


        IsSingleQueryResult     = resultObj.Count == 1;
        HasMultipleQueryResults = resultObj.Count > 1;
        IsSingleItem            = resultObj.Count == 1 && resultObj[0].ResultWrapped?.Count == 1;
        IsManyItem              = resultObj.Count == 1 && resultObj[0].ResultWrapped?.Count > 1;
        IsEmpty                 = resultObj.Count == 0 || !resultObj.Any(r => r.ResultWrapped != null || r.ResultWrapped?.Count > 0);

        return Result as List<DbQueryResult<TDataType?>>;
    }

    public bool AllOk()
    {
        var r = (List<DbQueryResult<TDataType?>>) Result!;
        if (r == null!) return false;
        if (r.Count == 0) return false;

        return r.All(x => x.Status == "OK");
    }

    public TDataType? First()
    {
        var data = ParseResponse();

        var qResult = data?.FirstOrDefault();
        if (qResult == null) return default;
        if (qResult.ResultWrapped == null) return default;

        return qResult.ResultWrapped.EndResult.FirstOrDefault();
    }

    public List<TDataType?> Get()
    {
        var data = ParseResponse();
        if (data?.Count == 0 || data == null) return new();

        return data.SelectMany(x => x.ResultWrapped!.EndResult).ToList();
    }

    /// <summary>
    /// When running a query with multiple queries inside, there will be multiple query results.
    /// For example;
    /// USE NS x;
    /// select * from y;
    ///
    /// In this case, we only explicitly want the second query result.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public List<TDataType?> GetFrom(int index)
    {
        var data = ParseResponse();
        if (data?.Count == 0 || data == null) return new();
        if (data.Count < index) return new();

        return data[index].ResultWrapped!.EndResult;
    }

    public TDataType? Last()
    {
        var data    = ParseResponse();
        var qResult = data?.LastOrDefault();
        if (qResult == null) return default;
        if (qResult.ResultWrapped == null) return default;

        return qResult.ResultWrapped.EndResult.LastOrDefault();
    }

    public List<ResultWrapper<TDataType?>?>? AllResponses()
    {
        var data = ParseResponse();

        return data?.Select(x => x.ResultWrapped)?.ToList();
    }

    public object? Process() => Process<object?>();

    public TObjectType? Process<TObjectType>()
    {
        return DbJson.Deserialize<TObjectType>(Result?.ToString()!);
    }

    public Type GetResultType() => typeof(TDataType);
}