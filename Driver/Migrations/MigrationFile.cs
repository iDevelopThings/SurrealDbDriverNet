using Driver.Migrations.History;
using Newtonsoft.Json;

namespace  Driver.Migrations;

public class MigrationFile
{
    public static string GetMigrationsDirPath()
    {
        return MigrationGenerator.MigrationsPath;
    }

    public static string GetMigrationsFilePath(string? fileName)
    {
        return Path.Join(GetMigrationsDirPath(), fileName ?? $"migration_{DateTime.Now.ToFileTime()}.srql");
    }

    public static (string filePath, string content) CreateMigrationFile(string content, string? fileName = null)
    {
        var filePath = GetMigrationsFilePath(fileName);
        if (File.Exists(filePath)) {
            throw new Exception($"Migration file already exists: {filePath}");
        }

        File.WriteAllText(filePath, content);

        return (filePath, content);
    }

    public static void Prepare()
    {
        if (!Directory.Exists(GetMigrationsDirPath())) {
            Directory.CreateDirectory(GetMigrationsDirPath());
        }
    }

    public static (string path, long date) WriteHistoryState(MigrationHistory history, long? date = null)
    {
        date ??= DateTime.Now.ToFileTime();

        var historyJson = JsonConvert.SerializeObject(history, Formatting.Indented);
        var (hPath, _) = CreateMigrationFile(historyJson, $"migration_{date}_state.json");

        return (hPath, date.Value);
    }

    public static (string path, long date) WriteMigration(string migrationContent, long? date = null)
    {
        date ??= DateTime.Now.ToFileTime();

        var (mPath, _) = CreateMigrationFile(migrationContent, $"migration_{date}.srql");

        return (mPath, date.Value);
    }

}