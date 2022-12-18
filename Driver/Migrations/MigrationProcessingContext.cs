using Driver.Migrations.Attributes;
using Driver.Migrations.Builder;

namespace Driver.Migrations;

public class MigrationStatement
{
    public bool IsDefineStatement { get; set; }
    public bool IsDropStatement   { get; set; }

    public string StatementType { get; }
    public string FieldName     { get; }

    public FieldStatement Statement { get; set; }

    public MigrationStatement(string statementType, string fieldName)
    {
        StatementType = statementType;
        FieldName     = fieldName;
    }

    public List<string> ToSql() => Statement.ToSql();

}

public class MigrationProcessingContext
{
    public MigrationAttribute       MigrationAttr { get; set; } = null!;
    public List<MigrationStatement> Statements    { get; set; } = new();

    public MigrationProcessingContext(IMigrationBase migration)
    {
        MigrationAttr = migration.GetMigrationAttribute();
    }

    public TStatement AddDefineStatement<TStatement>(string statementType, string fieldName)
        where TStatement : FieldStatement
    {
        var statementInstance = Activator.CreateInstance(typeof(TStatement), fieldName, MigrationAttr.Table, statementType) as FieldStatement;
        var statement = new MigrationStatement(statementType, fieldName) {
            IsDefineStatement = true,
            Statement         = statementInstance!,
        };

        Statements.Add(statement);

        return (statement.Statement as TStatement)!;
    }

    public TStatement AddDropStatement<TStatement>(string statementType, string fieldName)
        where TStatement : FieldStatement
    {
        var statementInstance = Activator.CreateInstance(typeof(TStatement), fieldName, MigrationAttr.Table, statementType) as FieldStatement;

        var statement = new MigrationStatement(statementType, fieldName) {
            IsDropStatement = true,
            Statement       = statementInstance!
        };

        Statements.Add(statement);

        return (statement.Statement as TStatement)!;
    }

    public MigrationStatement GetStatement(string fieldName)
    {
        return Statements.First(x => x.FieldName == fieldName);
    }


}