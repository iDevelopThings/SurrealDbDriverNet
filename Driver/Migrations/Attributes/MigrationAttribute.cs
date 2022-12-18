using Driver.Models.Utils;

namespace Driver.Migrations.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class MigrationAttribute : Attribute
{
    public Type?   ModelType   { get; }
    public string  Table       { get; }
    public string  Version     { get; }
    public string? Description { get; }

    public MigrationAttribute(string table, string version, string? description = null)
    {
        Table       = table;
        Version     = version;
        Description = description;
    }

    public MigrationAttribute(Type modelType, string version, string? description = null)
    {
        ModelType   = modelType;
        Table       = ModelUtils.GetTableName(modelType);
        Version     = version;
        Description = description;
    }

}