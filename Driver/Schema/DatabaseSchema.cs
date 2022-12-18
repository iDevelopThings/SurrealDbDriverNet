namespace Driver.Schema;

public class DatabaseSchema
{

    public Dictionary<string, DatabaseTable> Tables { get; set; } = new();

    public async Task<DatabaseTable> AddTable(string tableName, string tableDefineString)
    {
        var table = new DatabaseTable(tableName, tableDefineString);
        await table.Load();

        Tables.Add(tableName, table);

        return table;
    }

    public bool TableExists(string tableName)
    {
        return Tables.ContainsKey(tableName);
    }

    public DatabaseTable Table(string tableName)
    {
        return Tables[tableName];
    }
}