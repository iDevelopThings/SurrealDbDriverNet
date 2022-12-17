namespace Driver.Query.Grammar;

public class GrammarTokenList
{
    public List<GrammarToken> Tokens { get; set; } = new();

    public GrammarTokenListBuilder NewBuilder(Action<GrammarTokenListBuilder> cb)
    {
        var builder = new GrammarTokenListBuilder();
        cb(builder);

        return builder;
    }

    public GrammarTokenList Builder(Action<GrammarTokenListBuilder> cb)
    {
        var builder = new GrammarTokenListBuilder();
        cb(builder);

        AppendTokens(builder.Tokens);

        return this;
    }

    public void AppendTokens(List<GrammarToken> tokens) => AppendTokens(tokens.ToArray());

    public void AppendTokens(params GrammarToken[] tokens)
    {
        Tokens.AddRange(tokens);
    }
}

public class GrammarTokenListBuilder : GrammarTokenList
{
    public List<string> Values { get; set; } = new();

    public GrammarTokenListBuilder(params GrammarToken[] tokens)
    {
        if (tokens.Length > 0)
            Add(tokens);
    }

    public string Add(params GrammarToken[] tokens)
    {
        FixUpGrouping(tokens);

        foreach (var token in tokens) {
            Add(token);
        }

        return ToString();
    }

    private void FixUpGrouping(GrammarToken[] tokens)
    {
        if (tokens.Length == 0) return;

        var subQuery = tokens.FirstOrDefault(x => x.Type == GrammarTokenType.SubQuery);
        if (subQuery.Type == GrammarTokenType.SubQuery) {
            if (subQuery.Children.Count > 0) {
                // This prevents a query like:       v
                // select * from x where a = b and (|| c = d || e = f)
                if (subQuery.Children[0].ExprOperator != null) {
                    subQuery.Children.RemoveAt(0);
                }

                var subQueryTokens = subQuery.Children.ToArray();
                FixUpGrouping(subQueryTokens);
                subQuery.Children = subQueryTokens.ToList();
            }
        }
    }

    public string Add(GrammarToken token)
    {
        var value = token.Type switch {
            GrammarTokenType.Column         => AddSimpleToken(token),
            GrammarTokenType.Table          => AddSimpleToken(token),
            GrammarTokenType.Statement      => AddSimpleToken(token),
            GrammarTokenType.Operator       => AddSimpleToken(token),
            GrammarTokenType.Projection     => AddSimpleToken(token),
            GrammarTokenType.BoolExpression => AddBoolExpression(token),
            GrammarTokenType.Targets        => AddTargets(token),
            GrammarTokenType.Projections    => AddProjections(token),
            GrammarTokenType.Parameter      => AddParameter(token),
            GrammarTokenType.SubQuery       => AddSubQuery(token),
            GrammarTokenType.OrderByClauses => AddOrderByClauses(token),
            GrammarTokenType.OrderBy        => AddOrderBy(token),
            _                               => throw new Exception("Unknown token type: " + token.Type)
        };

        return value;
    }

    private string AddSubQuery(GrammarToken token)
    {
        var b = NewBuilder(builder => builder.Add(token.Children.ToArray()));

        var value = $"({b})";
        Values.Add(value);
        Tokens.Add(token);

        return value;
    }

    private string AddOrderByClauses(GrammarToken token)
    {
        if (token.Children.Count == 0) {
            throw new Exception("Order by must have at least one child");
        }

        var b = NewBuilder(builder => builder.Add(token.Children.ToArray()));

        var value = $"ORDER BY {string.Join(", ", b.Values)}";

        Values.Add(value);
        Tokens.Add(token);

        return value;
    }

    private string AddOrderBy(GrammarToken token)
    {
        if (token.Children.Count == 0) {
            throw new Exception("Order by must have at least one child");
        }

        var b = NewBuilder(builder => builder.Add(token.Children.ToArray()));

        var value = $"{b.Values[0]} {b.Values[1]}";

        Values.Add(value);
        Tokens.Add(token);

        return value;
    }

    private string AddParameter(GrammarToken token)
    {
        var value = "$" + token.Parameter!.VariableName;

        Values.Add(value);
        Tokens.Add(token);

        return value;
    }

    private string AddProjections(GrammarToken token)
    {
        if (token.Children.Count == 0) {
            token.Children.Add(new GrammarToken(GrammarTokenType.Column, "*"));
        }

        var b = NewBuilder(builder => builder.Add(token.Children.ToArray()));

        var value = token.Children.Count == 1
            ? b.ToString()
            : $"{string.Join(", ", b.Tokens.Select(x => x.Token))}";

        Values.Add(value);
        Tokens.Add(token);

        return value;
    }

    private string AddTargets(GrammarToken token)
    {
        var b = NewBuilder(builder => builder.Add(token.Children.ToArray()));

        var value = token.Children.Count == 1
            ? b.ToString()
            : $"{string.Join(", ", b.Tokens.Select(x => x.Token))}";

        Values.Add(value);
        Tokens.Add(token);

        return value;
    }

    private string AddSimpleToken(GrammarToken token)
    {
        Values.Add(token.Token);
        Tokens.Add(token);

        return token.Token;
    }

    private string AddBoolExpression(GrammarToken token)
    {
        Values.Add(token.ExprOperator!.Default());
        Tokens.Add(token);

        return token.Token;
    }

    public bool RequiresBoolExpression()
    {
        if (Tokens.Count == 0) return false;

        return !Tokens.Last().Type.HasFlag(GrammarTokenType.BoolExpression);
    }

    public override string ToString()
    {
        return string.Join(" ", Values);
    }

    public void Merge(GrammarTokenListBuilder builder)
    {
        Tokens.AddRange(builder.Tokens);
        Values.AddRange(builder.Values);
    }

    public string JoinValues(string glue = " AND ") => string.Join(glue, Values);
}