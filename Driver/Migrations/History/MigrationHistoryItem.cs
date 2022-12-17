using Driver.Migrations.Statements;
using Newtonsoft.Json;

namespace Driver.Migrations.History;

public class MigrationHistoryItem
{
    [JsonProperty("table")]
    public List<DefineTableStatement> Table { get; set; } = new();

    [JsonProperty("fields")]
    public List<DefineFieldStatement> Fields { get; set; } = new();

}