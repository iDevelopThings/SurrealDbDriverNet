using System.Reflection;
using Driver.Migrations.Attributes;
using Driver.Migrations.Builder;

namespace Driver.Migrations;

public interface IMigration<out TMigration> where TMigration : IMigration<TMigration>
{
}

public interface IMigrationBase
{
    MigrationProcessingContext Context { get; }

    void Up();

    void Down();

    string Build();

    public MigrationAttribute GetMigrationAttribute();

    public List<string> GetSqlStatements();

    public string GetIdStr();
}

public abstract class Migration<TMigration> : IMigration<TMigration>, IMigrationBase
    where TMigration : class, IMigration<TMigration>
{
    public  MigrationProcessingContext Context            { get; }
    private MigrationAttribute         MigrationAttribute { get; set; }

    private bool         _wasBuilt      = false;
    private List<string> _sqlStatements = new();
    private string       _sql           = null!;

    protected Migration()
    {
        Context = new MigrationProcessingContext(this);
    }

    public MigrationAttribute GetMigrationAttribute()
    {
        if (MigrationAttribute != null!)
            return MigrationAttribute;

        var migrationAttribute = GetType().GetCustomAttribute<MigrationAttribute>();
        if (migrationAttribute == null) {
            throw new Exception("Migration attribute not found");
        }

        return MigrationAttribute = migrationAttribute;
    }

    public abstract void Up();

    public abstract void Down();

    /*private TBuilder AddStatement<TBuilder>(string statementType, string fieldName, Dictionary<string, object>? parameters = null)
        where TBuilder : MigrationColumnDefinitionBase
    {
        return Context.AddStatement<TMigration, TBuilder>(statementType, fieldName, parameters);
    }*/

    public DropFieldStatement DropField(string fieldName)
        => Context.AddDropStatement<DropFieldStatement>(nameof(DropField), fieldName);

    public DefineFieldStatement Any(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Any), fieldName);

    public DefineArrayFieldStatement Array(string fieldName)
        => Context.AddDefineStatement<DefineArrayFieldStatement>(nameof(Array), fieldName);

    public DefineFieldStatement Bool(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Bool), fieldName);

    public DefineFieldStatement Datetime(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Datetime), fieldName);

    public DefineFieldStatement Decimal(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Decimal), fieldName);

    public DefineFieldStatement Duration(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Duration), fieldName);

    public DefineFieldStatement Float(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Float), fieldName);

    public DefineFieldStatement Int(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Int), fieldName);

    public DefineFieldStatement Number(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Number), fieldName);

    public DefineFieldStatement Object(string fieldName)
        => Context.AddDefineStatement<DefineFieldStatement>(nameof(Object), fieldName);

    public DefineRecordStatement Record(string fieldName, params string[] tables)
        => Context.AddDefineStatement<DefineRecordStatement>(nameof(Record), fieldName /*, new() {{"tables", tables}}*/);

    public List<string> GetSqlStatements()
    {
        return _sqlStatements;
    }

    public string Build()
    {
        if (_wasBuilt)
            return _sql;

        var table = GetMigrationAttribute().Table;

        var sql = new List<string>();
        foreach (var statement in Context.Statements) {
            sql.AddRange(statement.ToSql());
        }

        var sqlString = string.Join(Environment.NewLine, sql);

        _sqlStatements = sql;
        _sql           = sqlString;
        _wasBuilt      = true;

        return sqlString;
    }

    public string GetIdStr()
    {
        var attr = GetMigrationAttribute();
        return $"{attr.Table}:{attr.Version}";
    }
}