using Driver.Schema.Parser;

namespace Driver.Schema;

public class DatabaseTable
{
    public string Name         { get; set; }
    public string DefineString { get; set; }

    public Dictionary<string, DatabaseField> Fields { get; set; } = new();

    public DatabaseTable(string tableName, string tableDefineString)
    {
        Name         = tableName;
        DefineString = tableDefineString;

        if (tableDefineString != null!) {
            SchemaParser.ParseTable(this, tableDefineString);
        }
    }

    public async Task Load()
    {
        var tableInfo = await Database.GetTableInfo(Name);
        if (tableInfo == null) {
            throw new Exception("Failed to load table information for table: " + Name);
        }

        foreach (var (fieldName, fieldDefineString) in tableInfo.Fields.Where(f => !f.Key.EndsWith("[*]"))) {
            var field = new DatabaseField(fieldName, fieldDefineString);
            Fields.Add(fieldName, field);

            if (field.Type == "array") {
                var child = tableInfo.Fields.Where(f => f.Key == $"{fieldName}[*]").Select(f => f.Value).FirstOrDefault();
                if (child != null) {
                    field.Child = new DatabaseField(fieldName, child);
                }
            }
        }
    }
}