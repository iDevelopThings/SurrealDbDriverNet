using Driver.Migrations.Statements;

namespace  Driver.Migrations.History;

public class MigrationHistoryChangesState
{
    public class MigrationTableInfo
    {
        public string               TableName      { get; set; } = null!;
        public string               ModelName      { get; set; } = null!;
        public DefineTableStatement TableStatement { get; set; } = null!;
        public Type                 Model          { get; set; } = null!;
    }

    private Dictionary<string, MigrationTableInfo>         _tables = new();
    private Dictionary<string, List<DefineFieldStatement>> _fields = new();

    public Dictionary<string, List<DefineFieldStatement>> newFields    = new();
    public Dictionary<string, List<DefineFieldStatement>> removeFields = new();

    public static MigrationHistoryChangesState CreateInitial(MigrationHistory history)
    {
        var inst = new MigrationHistoryChangesState();

        history.TableHashes.Keys.ToList().ForEach(modelName =>
        {
            var tableInfo = history.Items[modelName].Table.First();

            inst._tables.Add(modelName, new MigrationTableInfo() {
                Model          = tableInfo.ModelType,
                ModelName      = tableInfo.ModelName,
                TableName      = tableInfo.Name,
                TableStatement = tableInfo,
            });

            inst._fields.Add(modelName, new List<DefineFieldStatement>());

            inst.BuildState(history, modelName);
        });

        return inst;
    }

    private void BuildState(MigrationHistory history, string modelName)
    {
        var modelHistory = history.Items[modelName];

        foreach (var statement in modelHistory.Fields) {
            // if (statement is DefineFieldStatement defineFieldStatement) {
            _fields[modelName].Add(statement);
            // }
        }
    }

    public MigrationHistoryChangesState RebuildFromChanges(MigrationHistoryChanges changes, bool isLatest = false)
    {
        this.BuildFromChanges(changes, isLatest);

        return this;
    }

    private void BuildFromChanges(MigrationHistoryChanges changes, bool isLatest)
    {
        if (changes.AddedTables.Count > 0) {
            changes.AddedTables.ForEach(modelName =>
            {
                if (!_fields.ContainsKey(modelName))
                    _fields.Add(modelName, new List<DefineFieldStatement>());
            });
        }

        if (changes.RemovedTables.Count > 0) {
            changes.RemovedTables.ForEach(modelName =>
            {
                if (_fields.ContainsKey(modelName))
                    _fields.Remove(modelName);
            });
        }

        _fields.Keys.ToList().ForEach(modelName =>
        {
            if (!changes.ChangedTables.Contains(modelName)) {
                return;
            }

            if (!_fields.ContainsKey(modelName))
                _fields.Add(modelName, new List<DefineFieldStatement>());

            if (changes.AddedFields.ContainsKey(modelName)) {
                _fields[modelName].AddRange(changes.AddedFields[modelName]);

                if (isLatest) {
                    if (!newFields.ContainsKey(modelName))
                        newFields.Add(modelName, new List<DefineFieldStatement>());

                    newFields[modelName].AddRange(changes.AddedFields[modelName]);
                }
            }

            if (changes.RemovedFields.ContainsKey(modelName)) {
                var removedFields = changes.RemovedFieldNames[modelName];
                var fields        = _fields[modelName];
                var newFields     = new List<DefineFieldStatement>();
                foreach (var field in fields) {
                    if (!removedFields.Contains(field.Name))
                        newFields.Add(field);
                }

                _fields[modelName] = newFields;

                if (isLatest) {
                    if (!removeFields.ContainsKey(modelName))
                        removeFields.Add(modelName, new List<DefineFieldStatement>());

                    removeFields[modelName].AddRange(changes.RemovedFields[modelName]);
                }
            }
        });
    }

    public void AddChangesLog(MigrationHistoryChanges changes)
    {
        Changes.Add(changes);
    }

    public List<MigrationHistoryChanges> Changes { get; set; } = new();

    public string ToMigration()
    {
        var statements = new List<string>();

        foreach (var (key, table) in _tables) {
            if (!NeedsMigrationForChanges(key)) {
                continue;
            }

            statements.Add("\n");
            statements.Add("// Statements for Model: " + table.ModelName);
            statements.Add("// Fields added:");
            foreach (var statement in newFields[key]) {
                statements.Add(statement.ToSql()!);
            }
            statements.Add("// Fields removed:");
            foreach (var statement in removeFields[key]) {
                statements.Add(statement.ToRemoveSql()!);
            }
        }

        return string.Join("\n", statements);
    }

    private bool NeedsMigrationForChanges(string tableName)
    {
        if (newFields.ContainsKey(tableName)) {
            if (newFields[tableName].Count > 0)
                return true;
        }

        if (removeFields.ContainsKey(tableName)) {
            if (removeFields[tableName].Count > 0)
                return true;
        }

        return false;
    }
}