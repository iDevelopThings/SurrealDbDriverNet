using System.Text;

namespace Driver.Migrations.Builder;

public class DropFieldStatement : FieldStatement
{
    public DropFieldStatement(string name, string tableName, string type) : base(name, tableName, type)
    {
    }

    public override string ToBaseSql(Action<StringBuilder>? typeCbModifier = null)
    {
        var sb = new StringBuilder();
        sb.Append("REMOVE FIELD ");
        sb.Append(Name);
        sb.Append(" ON ");
        sb.Append(TableName);
        sb.Append(";");

        return sb.ToString();
    }

    public override List<string> ToSql()
    {
        return new() {ToBaseSql()};
    }

}