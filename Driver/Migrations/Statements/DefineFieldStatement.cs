using Driver.Models.Utils;

namespace Driver.Migrations.Statements;

public class DefineFieldStatement : DefineStatement
{
    public string              Name      { get; set; } = null!;
    public ModelFieldTypeInfo? Type      { get; set; }
    public string              TableName { get; set; } = null!;

    public DefineFieldStatement()
    {
        Kind = DefineKind.Field;
    }

    public string? ToRemoveSql()
    {
        var str = $"REMOVE FIELD {Name} ON {TableName}";
        return str + ";";
    }

    public override string? ToSql()
    {
        // DEFINE FIELD location ON users TYPE object;

        var str = $"DEFINE FIELD {Name} ON {TableName}";
        if (Type != null) {
            str += " TYPE ";

            str += Type.ToSqlString();
        }

        return str + ";";
    }

    public bool IsDifferent(DefineFieldStatement other) => ToSql() != other.ToSql();

    public static bool Equals(DefineFieldStatement? left, DefineFieldStatement? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        return !left.IsDifferent(right);
    }
}