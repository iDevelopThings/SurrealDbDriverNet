using System.Linq.Expressions;
using System.Reflection;
using Driver.Json;
using Driver.Query.Grammar;
using Driver.Query.Grammar.Clauses;
using Driver.Models;
using Driver.Models.Types;
using Driver.Models.Utils;
using Driver.Rpc.Response;
using Newtonsoft.Json;
using Driver.Reflection;

namespace Driver.Query;

public interface IBaseQueryBuilder
{
    public List<Thing>             Targets     { get; set; } 
    public List<QueryParameter>    Parameters  { get; set; } 
    public List<QuerySegment>      Segments    { get; set; } 
    public List<QueryProjection>   Projections { get; set; } 
    public GrammarTokenListBuilder Tokens      { get; set; } 
    public List<QueryOrder>        Orders      { get; set; } 
    public string?                 QueryString { get; set; }
    public QueryGrammar            Grammar     { get; set; } 
}

public interface IQueryBuilder<TModel> : IBaseQueryBuilder where TModel : class, ISurrealModel
{
    QueryBuilder<TModel> From(Thing thing);

    QueryBuilder<TModel> From(params Thing[] things);

    QueryBuilder<TModel> From(IEnumerable<Thing> things);

    QueryBuilder<TModel> OrderBy(string column, OrderDirection direction);

    QueryBuilder<TModel> OrderByAsc(string column);

    QueryBuilder<TModel> OrderByDesc(string column);

    QueryBuilder<TModel> Contains<T>(string column, List<T> values, WhereConnector connector = WhereConnector.And);

    QueryBuilder<TModel> Select(string column, string? alias = null, Action<QueryProjection>? cb = null);

    QueryBuilder<TModel> GeoDistance(string locationField, Action<GeoDistanceClause> geoDistanceBuilder, string? alias = null);

    QueryBuilder<TModel> Where<T>(string column, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder<TModel> Where(Expression<Func<TModel, object>> columnExpr, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder<TModel> Where(string column, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder<TModel> Where(string column, ExpressionOperator? op, object? value, WhereConnector connector = WhereConnector.And);

    QueryBuilder<TModel> When(bool condition, Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And);

    QueryBuilder<TModel> Grouped(Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And);

    string BuildSelect();

    Dictionary<string, object?>? GetParameters();

    string ToSql();

    Task<DbQueryResponse<TModel?>> Execute();

    Task<List<TModel?>> Get();

    Task<TModel?> First();
}

public class QueryBuilder<TModel> : IQueryBuilder<TModel> where TModel : class, ISurrealModel
{
    public List<Thing>             Targets     { get; set; } = null!;
    public List<QueryParameter>    Parameters  { get; set; } = null!;
    public List<QuerySegment>      Segments    { get; set; } = null!;
    public List<QueryProjection>   Projections { get; set; } = null!;
    public GrammarTokenListBuilder Tokens      { get; set; } = null!;
    public List<QueryOrder>        Orders      { get; set; } = null!;
    public string?                 QueryString { get; set; }
    public QueryGrammar            Grammar     { get; set; } = null!;

    public QueryBuilder()
    {
        From(ModelData<TModel>.GetTableThing());
    }

    private QueryParameter AddParameter(string variableName, object value)
    {
        var parameter = new QueryParameter {
            VariableName = $"{variableName}_{Parameters.Count}",
            Value        = value
        };

        Parameters.Add(parameter);

        return parameter;
    }


    public QueryBuilder<TModel> From(Thing thing)
    {
        Targets.Add(thing);
        return this;
    }

    public QueryBuilder<TModel> From(params Thing[] things)
    {
        Targets.AddRange(things);
        return this;
    }

    public QueryBuilder<TModel> From(IEnumerable<Thing> things)
    {
        Targets.AddRange(things);
        return this;
    }

    public QueryBuilder<TModel> OrderBy(string column, OrderDirection direction)
    {
        Orders.Add(new QueryOrder {Column = column, Direction = direction});

        return this;
    }

    public QueryBuilder<TModel> OrderByAsc(string column) => OrderBy(column, OrderDirection.Ascending);

    public QueryBuilder<TModel> OrderByDesc(string column) => OrderBy(column, OrderDirection.Descending);


    public new QueryBuilder<TModel> Contains<T>(string column, List<T> values, WhereConnector connector = WhereConnector.And)
    {
        var segment = new QuerySegment() {
            ColumnName = column,
            Type       = QuerySegmentType.Contains,
            Group      = QueryStatementGroup.Where,
            Operator   = ExpressionOperator.ContainsAny,
            Parameter  = AddParameter(column, (object) values)
        };

        segment.AddConnectorType(connector);

        Segments.Add(segment);

        return this;
    }

    public QueryBuilder<TModel> Select(string column, string? alias = null, Action<QueryProjection>? cb = null)
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

    public QueryBuilder<TModel> GeoDistance(string locationField, Action<GeoDistanceClause> geoDistanceBuilder, string? alias = null)
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

    public QueryBuilder<TModel> Where<T>(string column, object? value, WhereConnector connector = WhereConnector.And)
        => Where(column, null, value, connector);

    public new QueryBuilder<TModel> Where(Expression<Func<TModel, object>> columnExpr, object? value, WhereConnector connector = WhereConnector.And)
    {
        var mapper = (MemberInfo type) =>
        {
            JsonPropertyAttribute jsonProp = null!;
            if (type is PropertyInfo propInfo) {
                jsonProp = propInfo.GetCustomAttribute<JsonPropertyAttribute>()!;
                if (jsonProp?.PropertyName != null) {
                    return jsonProp.PropertyName;
                }
            }

            return type.Name;
        };
        var colExpr = columnExpr.GetMemberInfo(mapper);

        return Where(colExpr.Name, value, connector);
    }

    public QueryBuilder<TModel> Where(string column, object? value, WhereConnector connector = WhereConnector.And)
        => Where(column, null, value, connector);

    public QueryBuilder<TModel> Where(string column, ExpressionOperator? op, object? value, WhereConnector connector = WhereConnector.And)
    {
        var segment = QuerySegment.Where(column, value!, op, connector);
        segment.Parameter = AddParameter(column, value!);

        Segments.Add(segment);

        return this;
    }

    public QueryBuilder<TModel> When(bool condition, Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And)
    {
        if (!condition) return this;

        return Grouped(builder, connector);
    }

    private QueryBuilder<TModel> ChildBuilder()
    {
        var qb = new QueryBuilder<TModel>();
        qb.Projections.AddRange(Projections);
        qb.Targets.AddRange(Targets);
        qb.Parameters.AddRange(Parameters);

        return qb;
    }

    public QueryBuilder<TModel> Grouped(Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And)
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

    public new async Task<DbQueryResponse<TModel?>> Execute()
    {
        var query      = BuildSelect();
        var parameters = GetParameters();

        var queryResult = await Database.Query<TModel>(query, parameters);
        if (queryResult.IsError()) {
            throw new Exception("Error executing query(query: " + query + "): " + queryResult.Error);
        }

        return queryResult;
    }

    public new async Task<List<TModel?>> Get()
    {
        var result = await Execute();
        var items  = result.Get();

        return items;
    }

    public new async Task<TModel?> First()
    {
        var result = await Execute();
        var item   = result.First();

        return item;
    }
}