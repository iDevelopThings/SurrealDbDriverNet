namespace Driver.Query.Grammar;

public class QuerySegment
{
    public string?             ColumnName { get; set; }
    public QueryStatementGroup Group      { get; set; }
    public QuerySegmentType    Type       { get; set; }
    public QueryParameter?     Parameter  { get; set; }
    public ExpressionOperator? Operator   { get; set; }

    public IBaseQueryBuilder? SubQuery { get; set; } = null;

    public static QuerySegment Where(string column, object value, ExpressionOperator? op = null, WhereConnector connector = WhereConnector.And)
    {
        var segment = new QuerySegment() {
            ColumnName = column,
            Type       = QuerySegmentType.Expression,
            Group      = QueryStatementGroup.Where,
            Operator   = op ?? ExpressionOperator.Equal,
        };

        segment.AddConnectorType(connector);

        return segment;
    }

    public void AddConnectorType(WhereConnector connector)
    {
        if (connector == WhereConnector.And) {
            Type |= QuerySegmentType.And;
        }

        if (connector == WhereConnector.Or) {
            Type |= QuerySegmentType.Or;
        }
    }

    public bool IsAndConnector()
    {
        return (Type & QuerySegmentType.And) == QuerySegmentType.And;
    }

    public bool IsOrConnector()
    {
        return (Type & QuerySegmentType.Or) == QuerySegmentType.Or;
    }
}

[Flags]
public enum QuerySegmentType
{
    Contains   = (1 << 1),
    SubQuery   = (1 << 2),
    Expression = (1 << 3),
    Projection = (1 << 4),
    OrderBy    = (1 << 5),

    And = (1 << 10),
    Or  = (1 << 11),
}

public enum QueryStatementGroup
{
    Where,
    Select,
    Order,
    Limit,
    Start,
    Fetch,
    Timeout,
    Parallel
}