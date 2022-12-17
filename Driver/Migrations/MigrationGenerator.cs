using Driver.Migrations.History;
using Driver.Migrations.Statements;
using Driver.Models;
using Driver.Models.Utils;
using JetBrains.Annotations;

namespace Driver.Migrations;

[UsedImplicitly]
public class MigrationGenerator
{
    public static string MigrationsPath { get; set; } = null!;

    public List<ModelStatement> Statements { get; set; } = new();
    public string               Generated  { get; set; } = null!;

    public void BuildStatements()
    {
        foreach (var model in ModelDriver.Models.Keys) {
            GenerateForModel(model);
        }

        Generated = ToSql();
    }

    public void GenerateForModel(Type model)
    {
        var attributes = ModelUtils.GetAllAttributes(model);
        var statements = new List<DefineStatement> {DefineStatement.Table(model)};

        DefineModelAttributes(model, attributes, statements);

        var mStatements = new ModelStatement {
            TableName  = ModelUtils.GetTableName(model),
            ModelType  = model,
            Statements = statements
        };

        Statements.Add(mStatements);
    }

    private void DefineModelAttributes(
        Type                                    model,
        List<ModelUtils.ModelPropertyAttribute> attributes,
        List<DefineStatement>                   statements,
        bool                                    isChildIteration = false
    )
    {
        foreach (var attr in attributes) {
            if (!isChildIteration) {
                if (attr.Property.DeclaringType != model) continue;
            }

            if (attr.Children.Count > 0) {
                DefineModelAttributes(model, attr.Children, statements, true);
            } else {
                var field = DefineStatement.Field(ModelUtils.GetTableName(model), attr);
                statements.Add(field);

                if (field.Type?.Kind == ModelFieldTypes.Array && field.Type.SubType != null && field.Type.SubKind != null) {
                    var subKind = field.Type.SubKind;

                    var subField = new DefineFieldStatement {
                        Name      = $"{field.Name}.*",
                        TableName = field.TableName,
                        Type      = subKind
                    };
                    statements.Add(subField);
                }
            }
        }
    }

    private void EnsureDirectoriesExist()
    {
        MigrationFile.Prepare();
    }

    public bool Build(string? path = null)
    {
        if (path == null) {
            MigrationsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..", "migrations"));
        }

        EnsureDirectoriesExist();

        this.BuildStatements();
        MigrationHistory.GetHistory();

        var currentHistory = MigrationHistory.Create(this);
        MigrationHistory.Add(currentHistory);

        if (MigrationHistory.CanCheckForChanges()) {
            var state = MigrationHistory.BuildMigrationChangesState();
            Generated = state!.ToMigration();
        }

        if (MigrationHistory.HasChanges(currentHistory)) {
            var (hPath, date) = MigrationFile.WriteHistoryState(currentHistory);
            var (mPath, _)    = MigrationFile.WriteMigration(Generated, date);

            Console.WriteLine(this.Generated);
            Console.WriteLine("Wrote migration file to: " + mPath);
            Console.WriteLine("Wrote migration state file to: " + hPath);

            return true;
        }

        return false;
    }


    public string ToSql()
    {
        var statements = new List<string>();

        foreach (var modelStatements in Statements) {
            statements.Add("\n");
            statements.Add("// Statements for Model: " + modelStatements.ModelType.Name);
            foreach (var statement in modelStatements.Statements) {
                statements.Add(statement.ToSql()!);
            }
        }

        return string.Join("\n", statements);
    }
}