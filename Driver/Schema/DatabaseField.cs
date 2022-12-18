using Driver.Schema.Parser;

namespace Driver.Schema;

public class DatabaseField
{

    public string FieldDefineString { get; set; }
    public string Name              { get; set; }

    public string Table      { get; set; } = null!;
    public string Type       { get; set; } = null!;
    public string RecordType { get; set; } = null!;
    public string Assert     { get; set; } = null!;

    public DatabaseField? Child { get; set; }

    public DatabaseField(string fieldName, string fieldDefineString)
    {
        Name              = fieldName;
        FieldDefineString = fieldDefineString;

        if (fieldDefineString != null!) {
            SchemaParser.ParseField(this, fieldDefineString);
        }
    }

}