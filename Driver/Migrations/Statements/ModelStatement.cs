namespace Driver.Migrations.Statements;

public class ModelStatement
{
    public string                TableName  { get; set; } = null!;
    public Type                  ModelType  { get; set; } = null!;
    public List<DefineStatement> Statements { get; set; } = new();
}