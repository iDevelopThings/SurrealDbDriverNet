using System.Collections;
using Driver.Json;
using Driver.Query.Grammar;
using Driver.Query.Grammar.Clauses;
using Driver.Models;
using Driver.Models.Types;
using Driver.Rpc.Response;

namespace Driver.Query;

public interface IQueryBuilder
{
    List<Thing>             Targets     { get; set; }
    List<QueryParameter>    Parameters  { get; set; }
    List<QuerySegment>      Segments    { get; set; }
    List<QueryProjection>   Projections { get; set; }
    GrammarTokenListBuilder Tokens      { get; set; }
    List<QueryOrder>        Orders      { get; set; }
    string?                 QueryString { get; set; }
    QueryGrammar            Grammar     { get; set; }

    QueryBuilder OrderBy(string column, OrderDirection direction);

    QueryBuilder OrderByAsc(string column);

    QueryBuilder OrderByDesc(string column);

    QueryBuilder Contains<T>(string column, List<T> values, WhereConnector connector = WhereConnector.And);

    QueryBuilder Select(string column, string? alias = null, Action<QueryProjection>? cb = null);

    QueryBuilder GeoDistance(string locationField, Action<GeoDistanceClause> geoDistanceBuilder, string? alias = null);

    QueryBuilder Where<T>(string column, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder Where(string column, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder Where(string column, ExpressionOperator? op, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder When(bool condition, Action<QueryBuilder> builder, WhereConnector connector = WhereConnector.And);

    QueryBuilder Grouped(Action<QueryBuilder> builder, WhereConnector connector = WhereConnector.And);

    string BuildSelect();

    Dictionary<string, object?>? GetParameters();

    public Task<DbQueryResponse<object?>> Execute();

    public Task<List<object?>> Get();

    public Task<object?> First();

    QueryBuilder From(Thing thing);

    QueryBuilder From(params Thing[] things);

    QueryBuilder From(IEnumerable<Thing> things);

    string ToSql();
}

public class QueryBuilder : IQueryBuilder
{
    public List<Thing> Targets { get; set; } = new();

    public List<QueryParameter> Parameters { get; set; } = new();

    public List<QuerySegment> Segments { get; set; } = new();

    public List<QueryProjection> Projections { get; set; } = new();

    public GrammarTokenListBuilder Tokens { get; set; } = new();

    public List<QueryOrder> Orders { get; set; } = new();

    public string? QueryString { get; set; } = string.Empty;

    public QueryGrammar Grammar { get; set; } = null!;

    private QueryParameter AddParameter(string variableName, object value)
    {
        var parameter = new QueryParameter {
            VariableName = $"{variableName}_{Parameters.Count}",
            Value        = value
        };

        Parameters.Add(parameter);

        return parameter;
    }

    public QueryBuilder From(Thing thing)
    {
        Targets.Add(thing);
        return this;
    }

    public QueryBuilder From(params Thing[] things)
    {
        Targets.AddRange(things);
        return this;
    }

    public QueryBuilder From(IEnumerable<Thing> things)
    {
        Targets.AddRange(things);
        return this;
    }

    public QueryBuilder Contains<T>(string column, List<T> values, WhereConnector connector = WhereConnector.And)
    {
        var segment = new QuerySegment() {
            ColumnName = column,
            Type       = QuerySegmentType.Contains,
            Group      = QueryStatementGroup.Where,
            Operator   = ExpressionOperator.ContainsAny,
            Parameter  = AddParameter(column, (object) values)
        };

        // if (Segments.Count > 0)
        segment.AddConnectorType(connector);

        Segments.Add(segment);

        return this;
    }

    public QueryBuilder Select(string column, string? alias = null, Action<QueryProjection>? cb = null)
    {
        var segment = new QuerySegment() {
            ColumnName = column,
            Type       = QuerySegmentType.Projection,
            Group      = QueryStatementGroup.Select,
        };

        Segments.Add(segment);

        var projection = new QueryProjection() {
            ColumnName = column,
            Alias      = alias,
        };
        cb?.Invoke(projection);

        Projections.Add(projection);

        return this;
    }

    public QueryBuilder GeoDistance(string locationField, Action<GeoDistanceClause> geoDistanceBuilder, string? alias = null)
    {
        var clause = new GeoDistanceClause(locationField);
        geoDistanceBuilder(clause);

        Select(locationField, alias, projection =>
        {
            projection.IsMethod   = true;
            projection.MethodType = QueryProjection.QueryProjectionMethodType.Geo_Distance;
            projection.Clause     = clause;
        });

        var col = alias ?? locationField;

        Where(col, ExpressionOperator.Lte, clause.MaxDistance);
        OrderByDesc(col);

        return this;
    }

    public QueryBuilder Where<T>(string column, object? value, WhereConnector connector = WhereConnector.And)
        => Where(column, null, value, connector);

    public QueryBuilder Where(string column, object? value, WhereConnector connector = WhereConnector.And)
        => Where(column, null, value, connector);

    public QueryBuilder Where(string column, ExpressionOperator? op, object? value, WhereConnector connector = WhereConnector.And)
    {
        var segment = QuerySegment.Where(column, value!, op, connector);
        segment.Parameter = AddParameter(column, value!);

        Segments.Add(segment);

        return this;
    }

    public QueryBuilder When(bool condition, Action<QueryBuilder> builder, WhereConnector connector = WhereConnector.And)
    {
        if (!condition) return this;

        return Grouped(builder, connector);
    }

    public QueryBuilder Grouped(Action<QueryBuilder> builder, WhereConnector connector = WhereConnector.And)
    {
        var qb = this.ChildBuilder();

        builder.Invoke(qb);

        var segment = new QuerySegment() {
            Type     = QuerySegmentType.SubQuery,
            Group    = QueryStatementGroup.Where,
            SubQuery = qb,
        };

        segment.AddConnectorType(connector);

        Segments.Add(segment);

        foreach (var p in qb.Parameters) {
            if (Parameters.Any(x => x.VariableName == p.VariableName)) continue;
            Parameters.Add(p);
        }

        return this;
    }

    private QueryBuilder ChildBuilder()
    {
        var qb = new QueryBuilder();
        qb.Projections.AddRange(Projections);
        qb.Targets.AddRange(Targets);
        qb.Parameters.AddRange(Parameters);

        return qb;
    }

    public QueryBuilder OrderBy(string column, OrderDirection direction)
    {
        Orders.Add(new QueryOrder {Column = column, Direction = direction});

        return this;
    }

    public QueryBuilder OrderByAsc(string column) => OrderBy(column, OrderDirection.Ascending);

    public QueryBuilder OrderByDesc(string column) => OrderBy(column, OrderDirection.Descending);

    public string BuildSelect()
    {
        var whereGrammar  = QueryGrammar.ProcessWhereClause(this);
        var selectGrammar = QueryGrammar.ProcessSelect(this, whereGrammar);
        this.Tokens.Merge(selectGrammar.selectTokens);

        Grammar     = selectGrammar;
        QueryString = selectGrammar.GetResult();

        return QueryString!;
    }

    public Dictionary<string, object?>? GetParameters()
    {
        var parameters = new Dictionary<string, object?>();

        if (Parameters.Count == 0)
            return parameters;

        foreach (var parameter in Parameters) {
            parameters.Add(parameter.VariableName, parameter.Value);
        }

        return parameters;
    }

    public string ToSql()
    {
        var q = Tokens.ToString();

        foreach (var p in Parameters) {
            q = q.Replace($"${p.VariableName}", DbJson.Serialize(p.Value));
        }

        return q;
    }

    public async Task<DbQueryResponse<object?>> Execute()
    {
        var query      = BuildSelect();
        var parameters = GetParameters();

        var queryResult = await Database.Query<object>(query, parameters);
        if (queryResult.IsError()) {
            throw new Exception("Error executing query(query: " + query + "): " + queryResult.Error);
        }

        return queryResult;
    }

    public async Task<List<object?>> Get()
    {
        var result = await Execute();
        var items  = result.Get();

        return items;
    }

    public async Task<object?> First()
    {
        var result = await Execute();
        var item   = result.First();

        return item;
    }

}