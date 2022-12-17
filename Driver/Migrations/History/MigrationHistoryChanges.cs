using Driver.Migrations.Statements;

namespace  Driver.Migrations.History;

public class MigrationHistoryChanges
{
    public MigrationHistory Prev    { get; set; } = null!;
    public MigrationHistory Current { get; set; } = null!;

    public List<string> RemovedTables { get; set; } = new();
    public List<string> AddedTables   { get; set; } = new();
    public List<string> ChangedTables { get; set; } = new();

    public Dictionary<string, List<DefineFieldStatement>> AddedFields       { get; set; } = new();
    public Dictionary<string, List<DefineFieldStatement>> RemovedFields     { get; set; } = new();
    public Dictionary<string, List<string>>               RemovedFieldNames { get; set; } = new();

    public string SimplifiedChangeLog
    {
        get
        {
            var addedList   = string.Join("\n", AddedFields.SelectMany(x => x.Value).Select(x => $"[{x.TableName}]" + x.Name).ToList());
            var removedList = string.Join("\n", RemovedFields.SelectMany(x => x.Value).Select(x => $"[{x.TableName}]" + x.Name).ToList());

            return "Added:\n" + addedList + "\n\nRemoved:\n" + removedList;
            // var removedList = string.Join("\n", RemovedFields.SelectMany(x => (x.Key, x.Value))
            // .Select(((table, value)) => $"[{table}]" + Value).ToList());
        }
    }

    public bool HasChanges()
    {
        return RemovedFields.Count > 0 ||
               AddedFields.Count > 0 ||
               ChangedTables.Count > 0 ||
               AddedTables.Count > 0 ||
               RemovedTables.Count > 0;
    }
}