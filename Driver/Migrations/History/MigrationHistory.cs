using System.Security.Cryptography;
using System.Text;
using Driver.Migrations.Statements;
using Newtonsoft.Json;
using Reflection;

namespace Driver.Migrations.History;

public partial class MigrationHistory
{
    private static List<MigrationHistory> _migrationHistories = null!;

    [JsonProperty("date")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("items")]
    public Dictionary<string, MigrationHistoryItem> Items { get; set; } = new();

    [JsonProperty("table_hashes")]
    public Dictionary<string, string> TableHashes { get; set; } = new();

    [JsonProperty("migration_hash")]
    public string MigrationHash { get; set; } = string.Empty;

    public static bool HasChanges(MigrationHistory newHistory)
    {
        var oldHistory = GetLastMigration();
        if (oldHistory == null)
            return true;

        return (oldHistory.MigrationHash != newHistory.MigrationHash);
    }

    public static string[] GetStateFiles()
    {
        var stateFiles = Directory.GetFiles(MigrationFile.GetMigrationsDirPath(), "*_state.json");
        if (stateFiles.Length == 0) {
            return Array.Empty<string>();
        }

        return stateFiles.OrderByDescending(Path.GetFileName).ToArray();
    }

    public static MigrationHistoryChangesState? BuildMigrationChangesState()
    {
        var migrationHistory = GetHistory().OrderBy(x => x.CreatedAt).ToList();
        if (migrationHistory.Count < 2)
            return null;

        var state = MigrationHistoryChangesState.CreateInitial(migrationHistory[0]);

        MigrationHistory prev = null!;
        for (int i = 0; i < migrationHistory.Count; i++) {
            if (i == 0) {
                prev = migrationHistory[i];
                continue;
            }

            var current = migrationHistory[i];
            var changes = FindDifferences(current, prev);

            if (changes.HasChanges()) {
                state.AddChangesLog(changes);
                state.RebuildFromChanges(changes, migrationHistory.Count - 1 == i);
            }
        }

        return state;
    }

    public static MigrationHistory? GetLastMigration()
    {
        var stateFiles = GetStateFiles();
        if (stateFiles.Length == 0) return null;

        return JsonConvert.DeserializeObject<MigrationHistory>(File.ReadAllText(stateFiles[0]));
    }

    public static List<MigrationHistory> GetHistory()
    {
        if (_migrationHistories != null!) {
            return _migrationHistories;
        }

        var stateFiles = GetStateFiles();
        var history    = new List<MigrationHistory>();
        foreach (var stateFile in stateFiles) {
            history.Add(JsonConvert.DeserializeObject<MigrationHistory>(File.ReadAllText(stateFile))!);
        }

        return _migrationHistories = history.OrderBy(h => h.CreatedAt).ToList();
    }

    public static MigrationHistory Create(MigrationGenerator migrator)
    {
        var inst = new MigrationHistory {
            CreatedAt = DateTime.Now
        };

        foreach (var modelStatement in migrator.Statements) {
            if (!inst.TableHashes.ContainsKey(modelStatement.TableName)) {
                // Create a hash of the table definition
                var hash = Hash(modelStatement.ModelType);
                inst.TableHashes.Add(modelStatement.TableName, hash.ToString());
            }

            if (!inst.Items.ContainsKey(modelStatement.TableName)) {
                inst.Items.Add(modelStatement.TableName, new MigrationHistoryItem());
            }

            foreach (var statement in modelStatement.Statements) {
                var historyItem = inst.Items[modelStatement.TableName];
                if (statement is DefineTableStatement tblStatement) {
                    historyItem.Table.Add(tblStatement);
                }

                if (statement is DefineFieldStatement fieldStatement) {
                    historyItem.Fields.Add(fieldStatement);
                }
            }
        }

        inst.MigrationHash = CreateSha256Hash(inst.TableHashes.Values.Aggregate((a, b) => a + b));
        return inst;
    }

    public static string Hash(Type model)
    {
        var seen       = new HashSet<object>();
        var properties = GetAllSimpleProperties(model, seen);

        var hash = properties.Select(p => Encoding.UTF8.GetBytes(p).AsEnumerable())
           .Aggregate((ag, next) => ag.Concat(next))
           .ToArray();

        return CreateSha256Hash(hash);
    }

    private static IEnumerable<string> GetAllSimpleProperties(Type model, HashSet<object> seen)
    {
        var props = PropertiesOf.All(model);

        foreach (var (key, property) in props) {
            var checker = new ObjectTypeChecker(property);
            if (checker.IsInt() || checker.IsLong() || checker.IsString() || checker.IsBool() || checker.IsDateTime()) {
                yield return key;
            } else if (seen.Add(property)) // Handle cyclic references
            {
                foreach (var simple in GetAllSimpleProperties(property, seen)) yield return simple;
            }
        }
    }


    private static class PropertiesOf
    {
        public static List<(string key, Type type)> All(Type model)
        {
            var props = new List<(string key, Type type)>();

            foreach (var property in model.GetProperties()) { 
                props.Add(($"{model.FullName}|{property.Name}|{property.PropertyType.Name}", property.PropertyType));
            }

            return props;
        }
    }

    private static MigrationHistoryChanges FindDifferences(MigrationHistory current, MigrationHistory prev)
    {
        var removedTables = prev.TableHashes.Keys.Except(current.TableHashes.Keys).ToList();
        var addedTables   = current.TableHashes.Keys.Except(prev.TableHashes.Keys).ToList();

        var changedTables = prev.TableHashes.Keys.Intersect(current.TableHashes.Keys)
           .Where(k => prev.TableHashes[k] != current.TableHashes[k])
           .ToList();

        var changes = new MigrationHistoryChanges() {
            Prev    = prev,
            Current = current,

            RemovedTables = removedTables,
            AddedTables   = addedTables,
            ChangedTables = changedTables,
        };
        new MigrationHistoryChanges() {
            Prev    = prev,
            Current = current,

            RemovedTables = removedTables,
            AddedTables   = addedTables,
            ChangedTables = changedTables,
            // RemovedFields = removedFields,
            // AddedFields   = addedFields,
            // RemovedItems  = removedItems,
            // AddedItems    = addedItems,
        };

        var tables = prev.TableHashes.Keys.Union(current.TableHashes.Keys).ToList();
        foreach (var table in tables) {
            var prevFields    = prev.Items[table].Fields.ToList();
            var currentFields = current.Items[table].Fields.ToList();
            
            var removedFields = prevFields.Where(o => currentFields.All(n => o.Name != n.Name)).ToList();
            var addedFields   = currentFields.Where(n => prevFields.All(o => o.Name != n.Name)).ToList();
            
            changes.AddedFields[table]       = addedFields;
            changes.RemovedFields[table]     = removedFields;
            changes.RemovedFieldNames[table] = removedFields.Select(f => f.Name).ToList();

        }

        return changes;
    }

    public static bool CanCheckForChanges()
    {
        return GetHistory().Count >= 2;
    }

    public static void Add(MigrationHistory currentHistory)
    {
        GetHistory();
        _migrationHistories.Add(currentHistory);
    }
    
    public static string CreateSha256Hash(byte[] content)
    {
        using var sha256Hash = SHA256.Create();
        var       bytes      = sha256Hash.ComputeHash(content);

        var builder = new StringBuilder();
        for (var i = 0; i < bytes.Length; i++) {
            builder.Append(bytes[i].ToString("x2"));
        }

        return builder.ToString();
    }
    
    public static string CreateSha256Hash(string content)
    {
        return CreateSha256Hash(Encoding.UTF8.GetBytes(content));
    }
}