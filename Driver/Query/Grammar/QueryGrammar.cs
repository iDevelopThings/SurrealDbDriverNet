namespace Driver.Query.Grammar;

[Flags]
public enum GrammarType
{
    None   = (1 << 0),
    Select = (1 << 1),
    Where  = (1 << 2),
}

public enum WhereConnector
{
    And,
    Or
}

public class QueryGrammar
{
    private readonly QueryBuilder _builder;

    public GrammarType GrammarType { get; set; }

    public GrammarTokenListBuilder whereTokens = null!;
    public GrammarTokenListBuilder selectTokens = null!;

    private QueryGrammar(QueryBuilder builder)
    {
        _builder = builder;
    }

    private GrammarTokenListBuilder BuildTokens_Segment(QuerySegment segment)
    {
        if ((segment.Type & QuerySegmentType.Contains) != 0) {
            return GrammarToken.ContainsAny(segment);
        }

        if ((segment.Type & QuerySegmentType.Expression) != 0) {
            return GrammarToken.Expression(segment);
        }

        if ((segment.Type & QuerySegmentType.SubQuery) != 0) {
            var subBuilder = BuildTokens_WhereClause(segment.SubQuery!);
            return GrammarToken.SubQuery(segment, subBuilder.Tokens);
        }

        throw new Exception("Unknown segment type: " + segment.Type);
    }

    public GrammarTokenListBuilder BuildTokens_WhereClause(QueryBuilder builder)
    {
        var segments = builder.Segments.Where(x => x.Group == QueryStatementGroup.Where).ToList();
        if (segments.Count == 0) {
            throw new Exception("No where clause segments found");
        }

        var b = new GrammarTokenListBuilder();

        var idx = 0;
        foreach (var segment in segments) {
            if (idx > 0) {
                if ((segment.Type & QuerySegmentType.And) != 0) {
                    b.Add(new GrammarToken {
                        Type         = GrammarTokenType.BoolExpression,
                        ExprOperator = ExpressionOperator.And
                    });
                } else if ((segment.Type & QuerySegmentType.Or) != 0) {
                    b.Add(new GrammarToken {
                        Type         = GrammarTokenType.BoolExpression,
                        ExprOperator = ExpressionOperator.Or
                    });
                }
            }

            b.Merge(BuildTokens_Segment(segment));
            idx++;
        }

        return b;
    }

    public static QueryGrammar ProcessSelect(QueryBuilder builder, QueryGrammar whereClause)
    {
        var grammar = new QueryGrammar(builder) {
            GrammarType    = GrammarType.Select,
        };

        var projections = new List<GrammarToken>();
        foreach (var projection in builder.Projections) {
            var segments = new List<string>();

            var column = projection.ColumnName;
            if (projection is {IsMethod: true, Clause: QueryProjectMethod method}) {
                column = method.BuildFunction();
            }

            segments.Add(column!);

            if (projection.Alias != null) {
                segments.Add("AS");
                segments.Add(projection.Alias);
            }

            var result = string.Join(" ", segments);

            projections.Add(
                new GrammarToken(GrammarTokenType.Projection, result)
            );
        }

        var selectBuilder = new GrammarTokenListBuilder(
            new GrammarToken(GrammarTokenType.Statement, "SELECT"),
            new GrammarToken(GrammarTokenType.Projections) {Children = projections},
            new GrammarToken(GrammarTokenType.Statement, "FROM"),
            new GrammarToken {
                Type     = GrammarTokenType.Targets,
                Children = builder.Targets.Select(t => new GrammarToken(GrammarTokenType.Table, t.ToString())).ToList()
            }
        );

        selectBuilder.Add(new GrammarToken(GrammarTokenType.Statement, "WHERE"));
        selectBuilder.Merge(whereClause.whereTokens);

        if (builder.Orders.Count > 0) {
            selectBuilder.Add(
                new GrammarToken(
                    GrammarTokenType.OrderByClauses,
                    "ORDER BY",
                    builder.Orders.Select(GrammarToken.OrderBy).ToList()
                )
            );
        }

        // selectBuilder.Add(new GrammarToken(GrammarTokenType.Statement, "LIMIT 20"));

        grammar.selectTokens = selectBuilder;

        return grammar;
    }

    public static QueryGrammar ProcessWhereClause(QueryBuilder builder)
    {
        var grammar = new QueryGrammar(builder) {
            GrammarType    = GrammarType.Where,
        };

        grammar.whereTokens = grammar.BuildTokens_WhereClause(builder);

        return grammar;
    }

    public string? GetResult()
    {
        return GrammarType switch {
            GrammarType.Select => selectTokens.ToString(),
            GrammarType.None   => throw new ArgumentOutOfRangeException(),
            _                  => throw new ArgumentOutOfRangeException()
        };
    }
}