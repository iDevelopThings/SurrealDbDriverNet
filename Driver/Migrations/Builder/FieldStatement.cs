using System.Text;

namespace Driver.Migrations.Builder;

public class FieldStatement
{
    public string Name      { get; set; }
    public string TableName { get; set; }
    public string Type      { get; set; }

    public FieldStatement(string name, string tableName, string type)
    {
        Name      = name;
        TableName = tableName;
        Type      = type;
    }

    public virtual string ToBaseSql(Action<StringBuilder>? typeCbModifier = null)
    {
        var sb = new StringBuilder();
        sb.Append("DEFINE FIELD ");
        sb.Append(Name);
        sb.Append(" ON ");
        sb.Append(TableName);
        if (Type != null!) {
            sb.Append(" TYPE ");
            if (typeCbModifier != null) {
                typeCbModifier(sb);
            } else {
                sb.Append(Type.ToLower());
            }
        }

        sb.Append(";");

        return sb.ToString();
    }

    public virtual List<string> ToSql()
    {
        return new() {
            ToBaseSql()
        };
    }
}