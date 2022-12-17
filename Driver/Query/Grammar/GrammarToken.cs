namespace Driver.Query.Grammar;

[Flags]
public enum GrammarTokenType
{
    Column         = (1 << 0),
    RawValue       = (1 << 1),
    Parameter      = (1 << 2),
    Operator       = (1 << 3),
    BoolExpression = (1 << 4),
    SubQuery       = (1 << 5),

    Statement   = (1 << 6),
    Targets     = (1 << 7),
    Table       = (1 << 8),
    Projections = (1 << 9),
    Projection  = (1 << 10),

    OrderByClauses = (1 << 11),
    OrderBy        = (1 << 12),
}

public struct GrammarToken
{
    public GrammarTokenType   Type      { get; set; }
    public string             Token     { get; set; }
    public QueryParameter?    Parameter { get; set; }
    public List<GrammarToken> Children  { get; set; }

    public ExpressionOperator? ExprOperator { get; set; }

    public GrammarToken(GrammarTokenType type, string token)
    {
        Type         = type;
        Token        = token;
        Children     = new List<GrammarToken>();
        Parameter    = null;
        ExprOperator = null;
    }

    public GrammarToken(GrammarTokenType type) : this(type, null!)
    {
    }

    public GrammarToken(GrammarTokenType type, string token, List<GrammarToken> children)
    {
        Type         = type;
        Token        = token;
        Children     = children;
        Parameter    = null;
        ExprOperator = null;
    }

    public GrammarToken(QueryParameter parameter)
    {
        Type         = GrammarTokenType.Parameter;
        Token        = $"${parameter.VariableName}";
        Children     = new List<GrammarToken>();
        Parameter    = parameter;
        ExprOperator = null;
    }

    public static GrammarTokenListBuilder ContainsAny(QuerySegment segment)
    {
        var list = new GrammarTokenListBuilder(
            new GrammarToken(GrammarTokenType.Column, segment.ColumnName!),
            new GrammarToken(GrammarTokenType.Operator, segment.Operator!.Default()),
            new GrammarToken(segment.Parameter!)
        );

        if (segment.IsAndConnector()) {
            list.Add(new GrammarToken {
                ExprOperator = ExpressionOperator.And,
                Type         = GrammarTokenType.BoolExpression,
                Token        = ExpressionOperator.And.Default(),
            });
        } else if (segment.IsOrConnector()) {
            list.Add(new GrammarToken {
                ExprOperator = ExpressionOperator.Or,
                Type         = GrammarTokenType.BoolExpression,
                Token        = ExpressionOperator.Or.Default(),
            });
        }

        return list;
    }

    public static GrammarTokenListBuilder Expression(QuerySegment segment)
    {
        return new GrammarTokenListBuilder(
            new GrammarToken(GrammarTokenType.Column, segment.ColumnName!),
            new GrammarToken(GrammarTokenType.Operator, segment.Operator!.Default()),
            new GrammarToken(segment.Parameter!)
        );
    }

    public static GrammarToken OrderBy(QueryOrder order)
    {
        return new GrammarToken(GrammarTokenType.OrderBy) {
            Children = new() {
                new GrammarToken(GrammarTokenType.Column, order.Column!),
                new GrammarToken(GrammarTokenType.Statement, order.Direction.ToDescription())
            }
        };
    }

    public static GrammarTokenListBuilder SubQuery(QuerySegment segment, List<GrammarToken> children)
    {
        return new GrammarTokenListBuilder(
            new GrammarToken {Type = GrammarTokenType.SubQuery, Children = children}
        );
    }


}