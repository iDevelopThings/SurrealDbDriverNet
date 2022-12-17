namespace Driver.Query.Grammar;

public interface QueryProjectMethod
{
    public string BuildFunction();
}

public class QueryProjection
{
    public string?                   Alias      { get; set; }
    public string?                   ColumnName { get; set; }
    public bool                      IsMethod   { get; set; } = false;
    public QueryProjectionMethodType MethodType { get; set; } = QueryProjectionMethodType.None;
    public object?                   Clause     { get; set; }

    public enum QueryProjectionMethodType
    {
        None,
        Geo_Distance,
    }
}