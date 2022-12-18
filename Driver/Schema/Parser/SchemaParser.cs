using System.Text.RegularExpressions;

namespace Driver.Schema.Parser;

public class SchemaParser
{
    public enum DefineType
    {
        Table,
        Field
    }

    private static (string lineTrimmed, string[] tokens) ParseLine(string line)
    {
        var lineTrimmed = line.Trim();

        if (lineTrimmed.EndsWith(";")) {
            lineTrimmed = lineTrimmed.Substring(0, lineTrimmed.Length - 1);
        }

        var tokens = Tokenize(lineTrimmed);

        return (lineTrimmed, tokens);
    }

    private static DefineType GetDefinedType(string[] tokens)
    {
        if (tokens[0] != "DEFINE") {
            throw new Exception("Invalid DEFINE statement");
        }

        if (tokens[1] == "TABLE") {
            return DefineType.Table;
        } else if (tokens[1] == "FIELD") {
            return DefineType.Field;
        } else {
            throw new Exception("Invalid DEFINE statement");
        }
    }

    public static void ParseField(DatabaseField field, string line)
    {
        var (lineTrimmed, tokens) = ParseLine(line);
        if (tokens.Length < 2) {
            return;
        }

        var defineType = GetDefinedType(tokens);
        if (defineType != DefineType.Field) {
            return;
        }

        // The third token is the field name
        var fieldName = tokens[2];
        // Arrays end up looking something like this in the db:
        // DEFINE FIELD test_array ON testing TYPE array
        // DEFINE FIELD test_array[*] ON testing TYPE string
        /*if (fieldName.EndsWith("[*]")) {
            
        }*/
        // The fifth token is the table name
        var tableName = tokens[4];

        field.Table = tableName;
        field.Type  = "any";

        if (tokens.Length >= 6) {
            // The seventh token is the field type
            var fieldType = tokens[6];

            field.Type = fieldType;

            // Check if the field type is "record"
            if (fieldType.StartsWith("record")) {
                // The record type is the part of the field type after "record(" and before the closing parenthesis
                var recordType = fieldType.Substring("record(".Length, fieldType.Length - "record(".Length - 1);

                field.RecordType = recordType;
            }
        }

        if (tokens.Length >= 8) {
            // The ninth token is the field assert
            // Everything after this, is the assert, so we should grab all the tokens after the 8th token
            field.Assert = string.Join(" ", tokens.Skip(8));
        }
    }

    public static void ParseTable(DatabaseTable table, string line)
    {
        var (lineTrimmed, tokens) = ParseLine(line);
        if (tokens.Length < 2) {
            return;
        }

        var defineType = GetDefinedType(tokens);
        if (defineType != DefineType.Table) {
            return;
        }

        // The third token is the table name
        string tableName = tokens[2];
    }

    private static string[] Tokenize(string str)
    {
        // Split the string on whitespace characters
        return Regex.Split(str, @"\s+");
    }
}