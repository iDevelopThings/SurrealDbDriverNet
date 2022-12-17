using System.Reflection;
using Driver.Models.Types;
using Driver.Models.Utils;
using Driver.Query;
using Newtonsoft.Json;

namespace Driver.Models;

public interface ISurrealModel
{
    public Thing? Id { get; set; }

    public Type ModelType { get; }

    public string GetTableName();
}

public partial class SurrealModel<TModel> : ISurrealModel
    where TModel : class, ISurrealModel
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public Thing? Id { get; set; } = null;

    [JsonIgnore]
    public Type ModelType => GetType();

    public string GetTableName() => ModelUtils.GetTableName<TModel>();

    public Dictionary<string, (string, object?)> GetAttributes()
    {
        var attributes = new Dictionary<string, (string, object?)>();
        var properties = ModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties) {
            var pName = $"{property.Name}|{property.PropertyType.ToString()}";
            if (attributes.ContainsKey(pName)) {
                continue;
            }

            attributes.Add(pName, (property.Name, property.GetValue(this)));
        }

        return attributes;
    }

    public static QueryBuilder<TModel> Query() => new QueryBuilder<TModel>();

    public TModel SetAttributes(TModel model)
    {
        var properties = ModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties) {
            var value = property.GetValue(model);
            property.SetValue(this, value);
        }

        return (this as TModel)!;
    }

    public TModel SetAttributes(Dictionary<string, object?> attributes)
    {
        foreach (var (key, value) in attributes) {
            var property = ModelType.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null) continue;
            property.SetValue(this, value);
        }

        return (this as TModel)!;
    }

    public TModel SetAttributes(Dictionary<string, (string, object?)> attributes)
    {
        var properties = ModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties) {
            var pName = $"{property.Name}|{property.PropertyType.ToString()}";
            if (!attributes.ContainsKey(pName)) {
                continue;
            }

            if (property.CanWrite) {
                var (_, value) = attributes[pName];
                property.SetValue(this, value);
            }
        }

        return (this as TModel)!;
    }
}