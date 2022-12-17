using System.Reflection;
using System.Text;
using Driver.Models.Attributes;
using Newtonsoft.Json;

namespace Driver.Models.Utils;

public class ModelUtils
{
    public static string GetTableName(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<ModelAttribute>(false);
        if (tableAttribute == null || string.IsNullOrWhiteSpace(tableAttribute.Name)) {
            return CreateTableNameForType(type);
        }

        return tableAttribute.Name;
    }

    public static string GetTableName<T>() => GetTableName(typeof(T));

    public static string CreateTableNameForType(Type type)
    {
        var tableName = type.Name;
        if (tableName.EndsWith("Model")) {
            tableName = tableName.Substring(0, tableName.Length - 5);
        }

        // convert to snake case
        var sb = new StringBuilder();
        foreach (var ch in tableName) {
            if (char.IsUpper(ch)) {
                sb.Append('_');
                sb.Append(char.ToLower(ch));
            } else {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    public struct ModelPropertyAttribute
    {
        public PropertyInfo                 Property     { get; set; }
        public Type                         Type         { get; set; }
        public string                       PropertyName { get; set; }
        public string                       ColumnName   { get; set; }
        public List<Attribute>              Attributes   { get; set; }
        public List<ModelPropertyAttribute> Children     { get; set; }


        public ModelPropertyAttribute(PropertyInfo property)
        {
            Property     = property;
            Type         = property.PropertyType;
            PropertyName = property.Name;
            ColumnName   = property.Name;
            Attributes   = property.GetCustomAttributes().ToList();
            Children     = new List<ModelPropertyAttribute>();

            // Check if nullable type
            if (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                Type = Nullable.GetUnderlyingType(Type)!;
            }
        }
    }

    public static string GetColumnNameForProperty(PropertyInfo type)
    {
        return type.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? type.Name;
    }

    public static List<ModelPropertyAttribute> GetAllAttributes(Type modelType)
    {
        var attributes = new List<ModelPropertyAttribute>();
        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties) {
            var attr = new ModelPropertyAttribute(property) {
                ColumnName = GetColumnNameForProperty(property),
            };

            if (ModelFieldTypes.IsNestedPropertyType(property)) {
                attr.Children = GetNestedAttributes(modelType, property.PropertyType, attr.ColumnName + ".");
            }

            attributes.Add(attr);
        }

        return attributes;
    }

    public static List<ModelPropertyAttribute> GetNestedAttributes(Type modelType, Type propertyType, string propertyNamePrefix)
    {
        var attributes = new List<ModelPropertyAttribute>();
        var properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties) {
            var attr = new ModelPropertyAttribute(property) {
                ColumnName = propertyNamePrefix + GetColumnNameForProperty(property),
            };

            if (ModelFieldTypes.IsNestedPropertyType(property)) {
                attr.Children = GetNestedAttributes(
                    modelType,
                    property.PropertyType,
                    propertyNamePrefix + attr.ColumnName + "."
                );
            }

            attributes.Add(attr);
        }

        return attributes;
    }
}