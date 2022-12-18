using System.Reflection;
using Driver.Migrations.Attributes;

namespace Driver.Migrations;

public enum MigrationMethod
{
    Up,
    Down
}

public class Migrator
{
    public MigrationMethod     Method     { get; }
    public MigrationRepository Repository { get; set; }

    private int _batchId = -1;

    private List<Type> _migrationTypes = null!;

    private Dictionary<string, string>               _tableMigrationSql  = new();
    private Dictionary<string, IMigrationBase>       _tableMigrations    = new();
    private Dictionary<string, MigrationVersionMeta> _tableMigrationMeta = new();

    public Migrator(MigrationMethod method)
    {
        Method     = method;
        Repository = new MigrationRepository();
    }

    public void Add(IMigrationBase migration)
    {
        var attr = migration.GetMigrationAttribute();

        _tableMigrations.Add(attr.Table, migration);
    }

    public async Task Rollback()
    {
        _batchId = await Repository.GetLastBatchId();

        await LoadFromBatch();

        if (_tableMigrations.Count == 0) {
            Console.WriteLine("No migrations to rollback.");
            return;
        }

        await RunMigrationQueries();
    }

    public async Task Run()
    {
        _batchId = await Repository.GetNextBatchId();

        await LoadPendingMigrations();

        if (_tableMigrations.Count == 0) {
            Console.WriteLine("No migrations to run.");
            return;
        }

        await RunMigrationQueries();
    }

    private async Task RunMigrationQueries()
    {
        foreach (var (tableName, migration) in _tableMigrations) {
            var (sql, meta) = BeginProcessingMigration(tableName, migration);

            var (result, error) = await Repository.ExecuteMigrationSql(migration, sql, meta);
            if (error != null) {
                await Finalize();
                throw error;
            }

            if (!result) {
                await Finalize();
                throw new Exception($"Error running migration for table {tableName}");
            }

            _tableMigrationMeta.Add(tableName, meta);
        }

        await Finalize();
    }

    private (string sql, MigrationVersionMeta meta) BeginProcessingMigration(string tableName, IMigrationBase migration)
    {
        var attr = migration.GetMigrationAttribute();

        if (Method == MigrationMethod.Up) {
            migration.Up();
        } else {
            migration.Down();
        }

        var sql = migration.Build();
        _tableMigrationSql.Add(tableName, sql);

        var meta = new MigrationVersionMeta {
            BatchId       = _batchId,
            Table         = tableName,
            Version       = attr.Version,
            Description   = attr.Description!,
            SqlStatements = migration.GetSqlStatements(),
            IdStr         = migration.GetIdStr()
        };

        return (sql, meta);
    }

    private async Task Finalize()
    {
        if (Method == MigrationMethod.Up) {
            var result = await Database.Query(new List<string>() {
                "BEGIN TRANSACTION;",
                "USE NS _migrations;",
                "INSERT INTO migrations $objs;",
                "COMMIT TRANSACTION;",
            }, new() {{"objs", _tableMigrationMeta.Values.ToList()}});

            if (result!.IsError()) {
                throw new Exception($"Failed to finalize migration. Error: {result.Error!.Value.Message}");
            }

            return;
        }


        var ids = _tableMigrationMeta.Values.Select(v => v.IdStr).ToList();

        var vars = new Dictionary<string, object?>() {
            {"batch_id", _batchId},
        };

        var queries = new List<string>() {
            "BEGIN TRANSACTION;",
            "USE NS _migrations;",
        };

        var idx = 0;
        foreach (var id in ids) {
            queries.Add($"DELETE migrations WHERE batchId = $batch_id && idStr = $id_{idx};");
            vars.Add($"id_{idx}", id);
            idx++;
        }

        queries.Add("COMMIT TRANSACTION;");
        // "DELETE FROM migrations WHERE batch_id = $batch_id && idStr INSIDE $ids;",


        var downResult = await Database.Query(queries, vars);

        if (downResult!.IsError()) {
            throw new Exception($"Failed to finalize migration. Error: {downResult.Error!.Value.Message}");
        }
    }

    public List<Type> LoadAllMigrationTypes()
    {
        if (_migrationTypes != null!) {
            return _migrationTypes;
        }

        var ibase = typeof(IMigrationBase);
        var types = AppDomain.CurrentDomain.GetAssemblies()
           .SelectMany(s => s.GetTypes())
           .Where(p =>
            {
                if (p == ibase) return false;
                if (!p.IsClass) return false;
                if (p.IsAbstract) return false;

                return ibase.IsAssignableFrom(p) &&
                       p.GetCustomAttribute<MigrationAttribute>() != null;
            })
           .ToList();

        return _migrationTypes = types;
    }

    public List<IMigrationBase> LoadAllMigrations()
    {
        var types  = LoadAllMigrationTypes();
        var loaded = new List<IMigrationBase>();
        foreach (var type in types) {
            var migration = (IMigrationBase) Activator.CreateInstance(type)!;
            loaded.Add(migration!);
        }

        return loaded;
    }

    public async Task LoadPendingMigrations()
    {
        var loaded = LoadAllMigrations();
        var ran    = await Repository.GetRan();

        loaded = loaded.Where(migration => ran.All(m => m.IdStr != migration.GetIdStr())).ToList();

        foreach (var migration in loaded) {
            Add(migration);
        }
    }

    public async Task LoadFromBatch()
    {
        var loaded = LoadAllMigrations();
        var ran    = await Repository.GetMigrationsBatch(_batchId);

        loaded = loaded.Where(migration => ran.Exists(m => m.IdStr == migration.GetIdStr())).ToList();

        foreach (var migration in loaded) {
            Add(migration);
        }
    }

}