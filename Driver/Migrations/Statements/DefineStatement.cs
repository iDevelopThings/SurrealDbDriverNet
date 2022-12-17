using Driver.Models.Utils;

namespace Driver.Migrations.Statements;

public enum DefineKind
{
    Table,
    Field,
}

public class DefineStatement
{
    public DefineKind Kind { get; set; }

    public static DefineTableStatement Table(Type forModelType, bool schemafull = false)
    {
        return new DefineTableStatement {
            ModelName  = forModelType.Name,
            ModelType  = forModelType,
            Name       = ModelUtils.GetTableName(forModelType),
            Schemafull = schemafull,
        };
    }

    public static DefineFieldStatement Field(string tableName, ModelUtils.ModelPropertyAttribute attr)
    {
        return new DefineFieldStatement() {
            Name      = attr.ColumnName,
            TableName = tableName,
            Type      = ModelFieldTypes.From(attr.Type),
        };
    }

    public virtual string? ToSql()
    {
        return null;
    }

    public bool IsDifferent(DefineStatement other) => ToSql() != other.ToSql();

    public static bool Equals(DefineStatement? left, DefineStatement? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        return !left.IsDifferent(right);
    }

}