namespace Driver.Migrations.Builder;

public class DefineFieldStatement : FieldStatement
{
    public DefineFieldStatement(string name, string tableName, string type) : base(name, tableName, type)
    {
    }

}

public class DefineArrayFieldStatement : FieldStatement
{
    public string ArrayValueType { get; set; }


    public DefineArrayFieldStatement(string name, string tableName, string type) : base(name, tableName, type)
    {
    }

    public DefineArrayFieldStatement OfType(string type)
    {
        ArrayValueType = type;
        return this;
    }

    public override List<string> ToSql()
    {
        var child = new DefineFieldStatement(this.Name + ".*", this.TableName, this.ArrayValueType);

        return new() {
            ToBaseSql(sb => sb.Append("array")),
            child.ToBaseSql()
        };
    }

}

public class DefineRecordStatement : FieldStatement
{
    public List<string> RecordTypes { get; set; } = new();

    public DefineRecordStatement(string name, string tableName, string type) : base(name, tableName, type)
    {
    }

    public DefineRecordStatement OfType(string type)
    {
        RecordTypes.Add(type);
        return this;
    }

    public DefineRecordStatement OfType(params string[] type)
    {
        RecordTypes.AddRange(type);
        return this;
    }

    public override List<string> ToSql()
    {
        return new() {
            ToBaseSql(sb =>
            {
                if (RecordTypes.Count > 0) {
                    sb.Append("record(");
                    sb.Append(string.Join(", ", RecordTypes));
                    sb.Append(")");
                }
            })
        };
    }

}