using Newtonsoft.Json;

namespace Driver.Migrations;

public class MigrationVersionMeta
{
    [JsonProperty("table")]
    public string Table { get; set; } = null!;

    [JsonProperty("idStr")]
    public string IdStr { get; set; } = null!;

    [JsonProperty("version")]
    public string Version { get; set; } = null!;

    [JsonProperty("description")]
    public string Description { get; set; } = null!;

    [JsonProperty("statements")]
    public List<string> SqlStatements { get; set; } = new();

    [JsonProperty("batchId")]
    public int BatchId { get; set; }

    [JsonProperty("date")]
    public DateTime Date { get; set; }

    public MigrationVersionMeta()
    {
        Date = DateTime.Now;
    }
}

public class MigrationRepository
{
    public record MaxResult(int max);

    public async Task<int> GetLastBatchId()
    {
        var result = await Database.Query<MaxResult>(new List<string>() {
            "USE NS _migrations;",
            "select math::max(batchId) as max from migrations group by batchId;"
        });

        if (result!.IsError()) {
            throw new Exception($"Failed to get latest batch id. Error: {result.Error!.Value.Message}");
        }

        if (result.IsEmpty) {
            return 0;
        }

        var max = result.Last();
        if (max == null) {
            return 0;
        }

        return max!.max;
    }

    public async Task<int> GetNextBatchId()
    {
        var lastBatchId = await GetLastBatchId();
        return lastBatchId + 1;
    }

    public async Task<List<MigrationVersionMeta>> GetMigrationsBatch(int batchId)
    {
        var result = await Database.Query<MigrationVersionMeta>(new List<string>() {
            "USE NS _migrations;",
            "select * from migrations where batchId = $batchId order by batch, date asc;"
        }, new() {
            {"batchId", batchId}
        });

        if (result!.IsError()) {
            throw new Exception($"Failed to get ran migrations from db, error: {result.Error!.Value.Message}");
        }

        if (result == null! || result.IsEmpty) {
            return new();
        }

        return result.GetFrom(1)!;
    }

    public async Task<List<MigrationVersionMeta>> GetRan()
    {
        var result = await Database.Query<MigrationVersionMeta>(new List<string>() {
            "USE NS _migrations;",
            "select * from migrations order by batch, date asc;"
        });

        if (result!.IsError()) {
            throw new Exception($"Failed to get ran migrations from db, error: {result.Error!.Value.Message}");
        }

        if (result == null! || result.IsEmpty) {
            return new();
        }

        return result.GetFrom(1)!;
    }

    public async Task<(bool result, Exception? error)> ExecuteMigrationSql(IMigrationBase migration, string sql, MigrationVersionMeta meta)
    {
        var result = await Database.Query(new List<string>() {
            "BEGIN TRANSACTION;",
            sql,
            "COMMIT TRANSACTION;",
        });
        if (result!.IsError()) {
            return (false, new Exception($"Migration[{migration.GetType().Name}] failed. Error: {result.Error!.Value.Message}"));
        }

        if (!result!.AllOk()) {
            return (false, new Exception("Migration failed"));
        }

        var migrationName              = $"{migration.GetType().Name}(Table={migration.GetMigrationAttribute().Table}, Version={migration.GetMigrationAttribute().Version})";
        var doneStr                    = "✅ Done";
        var migrationNamePaddingString = new string('.', 20);

        var resultStr = $"{migrationName}{migrationNamePaddingString}{doneStr}";
        Console.WriteLine(resultStr);


        return (true, null);
    }
}