using System.Linq.Expressions;
using System.Reflection;
using Driver.Query.Grammar;
using Driver.Query.Grammar.Clauses;
using Driver.Models;
using Driver.Models.Types;
using Driver.Models.Utils;
using Driver.Rpc.Response;
using Newtonsoft.Json;
using Driver.Reflection;

namespace Driver.Query;

public interface IQueryBuilder<TModel> : IQueryBuilder where TModel : class, ISurrealModel
{
    public new QueryBuilder<TModel> OrderBy(string column, OrderDirection direction);

    public new QueryBuilder<TModel> OrderByAsc(string column);

    public new QueryBuilder<TModel> OrderByDesc(string column);

    public new QueryBuilder<TModel> Contains<T>(string column, List<T> values, WhereConnector connector = WhereConnector.And);

    public new QueryBuilder<TModel> Select(string column, string? alias = null, Action<QueryProjection>? cb = null);

    public new QueryBuilder<TModel> GeoDistance(string locationField, Action<GeoDistanceClause> geoDistanceBuilder, string? alias = null);

    public new QueryBuilder<TModel> Where<T>(string column, object? value, WhereConnector connector = WhereConnector.And);

    public new QueryBuilder<TModel> Where(string column, object? value, WhereConnector connector = WhereConnector.And);

    public new QueryBuilder<TModel> Where(string column, ExpressionOperator? op, object? value, WhereConnector connector = WhereConnector.And);

    public QueryBuilder<TModel> When(bool condition, Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And);

    public QueryBuilder<TModel> Grouped(Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And);

    public new string BuildSelect();

    public new Dictionary<string, object?>? GetParameters();

    public new Task<DbQueryResponse<TModel?>> Execute();

    public new Task<List<TModel?>> Get();

    public new Task<TModel?> First();

    public new QueryBuilder<TModel> From(Thing thing);

    public new QueryBuilder<TModel> From(params Thing[] things);

    public new QueryBuilder<TModel> From(IEnumerable<Thing> things);
}

public class QueryBuilder<TModel> : QueryBuilder, IQueryBuilder<TModel>
    where TModel : class, ISurrealModel
{
    public QueryBuilder()
    {
        From(ModelData<TModel>.GetTableThing());
    }

    public new QueryBuilder<TModel> From(Thing thing)
        => (QueryBuilder<TModel>) base.From(thing);

    public new QueryBuilder<TModel> From(params Thing[] things)
        => (QueryBuilder<TModel>) base.From(things);

    public new QueryBuilder<TModel> From(IEnumerable<Thing> things)
        => (QueryBuilder<TModel>) base.From(things);

    public new QueryBuilder<TModel> OrderBy(string column, OrderDirection direction)
        => (QueryBuilder<TModel>) base.OrderBy(column, direction);

    public new QueryBuilder<TModel> OrderByAsc(string column)
        => (QueryBuilder<TModel>) base.OrderByAsc(column);

    public new QueryBuilder<TModel> OrderByDesc(string column)
        => (QueryBuilder<TModel>) base.OrderByDesc(column);

    public new QueryBuilder<TModel> Contains<T>(string column, List<T> values, WhereConnector connector = WhereConnector.And)
        => (QueryBuilder<TModel>) base.Contains(column, values, connector);

    public new QueryBuilder<TModel> Select(string column, string? alias = null, Action<QueryProjection>? cb = null)
        => (QueryBuilder<TModel>) base.Select(column, alias, cb);

    public new QueryBuilder<TModel> GeoDistance(string locationField, Action<GeoDistanceClause> geoDistanceBuilder, string? alias = null)
        => (QueryBuilder<TModel>) base.GeoDistance(locationField, geoDistanceBuilder, alias);

    public new QueryBuilder<TModel> Where<T>(string column, object? value, WhereConnector connector = WhereConnector.And)
        => (QueryBuilder<TModel>) base.Where<T>(column, value, connector);

    public QueryBuilder<TModel> Where(Expression<Func<TModel, object>> columnExpr, object? value, WhereConnector connector = WhereConnector.And)
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

        return (QueryBuilder<TModel>) base.Where(colExpr.Name, value, connector);
    }

    public new QueryBuilder<TModel> Where(string column, object? value, WhereConnector connector = WhereConnector.And)
        => (QueryBuilder<TModel>) base.Where(column, value, connector);

    public new QueryBuilder<TModel> Where(string column, ExpressionOperator? op, object? value, WhereConnector connector = WhereConnector.And)
        => (QueryBuilder<TModel>) base.Where(column, op, value, connector);

    public QueryBuilder<TModel> When(bool condition, Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And)
        => (QueryBuilder<TModel>) base.When(condition, b => builder((QueryBuilder<TModel>) b), connector);

    public QueryBuilder<TModel> Grouped(Action<QueryBuilder<TModel>> builder, WhereConnector connector = WhereConnector.And)
        => (QueryBuilder<TModel>) base.Grouped(b => builder((QueryBuilder<TModel>) b), connector);

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