namespace Driver.Migrations.Statements;

public class DefineTableStatement : DefineStatement
{
    public string Name       { get; set; } = null!;
    public bool   Schemafull { get; set; } = false;
    public Type   ModelType  { get; set; } = null!;
    public string ModelName  { get; set; } = null!;

    public DefineTableStatement()
    {
        Kind = DefineKind.Table;
    }

    public override string? ToSql()
    {
        return $"DEFINE TABLE {Name} {(Schemafull ? "SCHEMAFULL" : "SCHEMALESS")};";
    }

    public bool IsDifferent(DefineTableStatement other) => ToSql() != other.ToSql();

    public static bool Equals(DefineTableStatement? left, DefineTableStatement? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        return !left.IsDifferent(right);
    }

}