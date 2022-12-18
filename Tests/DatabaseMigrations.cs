using Driver.Migrations;
using Driver.Migrations.Attributes;

namespace Tests;

/*
DEFINE FIELD test_any ON table_name TYPE any;
DEFINE FIELD test_array ON table_name TYPE array;
DEFINE FIELD test_boolean ON table_name TYPE boolean;
DEFINE FIELD test_datetime ON table_name TYPE datetime;
DEFINE FIELD test_decimal ON table_name TYPE decimal;
DEFINE FIELD test_duration ON table_name TYPE duration;
DEFINE FIELD test_float ON table_name TYPE float;
DEFINE FIELD test_integer ON table_name TYPE integer;
DEFINE FIELD test_number ON table_name TYPE number;
DEFINE FIELD test_object ON table_name TYPE object;
DEFINE FIELD test_record ON table_name TYPE record;
 */

public class DatabaseMigrationsTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Migration("testing", "237817")]
    public class TestMigrationClassOne : Migration<TestMigrationClassOne>
    {

        /// <inheritdoc />
        public override void Up()
        {
            Any("test_any");
            Array("test_array").OfType("string");
            Bool("test_boolean");
            Datetime("test_datetime");
            Decimal("test_decimal");
            Duration("test_duration");
            Float("test_float");
            Int("test_integer");
            Number("test_number");
            Object("test_object");
            Record("test_record").OfType("users", "organizations");
        }

        /// <inheritdoc />
        public override void Down()
        {
            DropField("test_any");
            DropField("test_array");
            DropField("test_boolean");
            DropField("test_datetime");
            DropField("test_decimal");
            DropField("test_duration");
            DropField("test_float");
            DropField("test_integer");
            DropField("test_number");
            DropField("test_object");
            DropField("test_record");
        }

    }

    [Migration("testing_another", "1234")]
    public class TestMigrationClassTwo : Migration<TestMigrationClassTwo>
    {

        /// <inheritdoc />
        public override void Up()
        {
            Any("test_any");
        }

        /// <inheritdoc />
        public override void Down()
        {
            DropField("test_any");
        }


    }

    [Test]
    public async Task TestMigrationsClass()
    {
        var migrationClass = new TestMigrationClassOne();
        migrationClass.Up();

        var definedStatementTypes = migrationClass.Context.Statements.Select(x => x.StatementType).ToList();
        Assert.Contains("Any", definedStatementTypes);
        Assert.Contains("Array", definedStatementTypes);
        Assert.Contains("Bool", definedStatementTypes);
        Assert.Contains("Datetime", definedStatementTypes);
        Assert.Contains("Decimal", definedStatementTypes);
        Assert.Contains("Duration", definedStatementTypes);
        Assert.Contains("Float", definedStatementTypes);
        Assert.Contains("Int", definedStatementTypes);
        Assert.Contains("Number", definedStatementTypes);
        Assert.Contains("Object", definedStatementTypes);
        Assert.Contains("Record", definedStatementTypes);

        Assert.IsTrue(migrationClass.Context.Statements.Any(x => x.FieldName == "test_array"));
    }

    [Test]
    public async Task TestMigrationClassSqlGeneration()
    {
        var migrationClass = new TestMigrationClassOne();
        migrationClass.Up();

        var sqlString = migrationClass.Build();
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_any ON testing TYPE any;"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_array ON testing TYPE array;"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_array.* ON testing TYPE string;"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_boolean ON testing TYPE bool"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_datetime ON testing TYPE datetime"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_decimal ON testing TYPE decimal"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_duration ON testing TYPE duration"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_float ON testing TYPE float;"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_integer ON testing TYPE int"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_number ON testing TYPE number;"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_object ON testing TYPE object;"));
        Assert.That(sqlString, Does.Contain("DEFINE FIELD test_record ON testing TYPE record(users, organizations);"));


        migrationClass = new TestMigrationClassOne();
        migrationClass.Down();
        sqlString = migrationClass.Build();

        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_any ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_array ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_boolean ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_datetime ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_decimal ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_duration ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_float ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_integer ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_number ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_object ON testing;"));
        Assert.That(sqlString, Does.Contain("REMOVE FIELD test_record ON testing;"));
    }

    [Test]
    public async Task TestMigratorRollback()
    {
        var migrator = new Migrator(MigrationMethod.Down);
        await migrator.Rollback();

        // await migrator.Run();
    }

    [Test]
    public async Task TestMigrator()
    {
        var migrator = new Migrator(MigrationMethod.Up);

        await migrator.Run();
    }

    [Test]
    public async Task TestGettingAllMigrations()
    {
        var migrator = new Migrator(MigrationMethod.Up);
        // migrator.Add(new TestMigrationClass());

        var migrations = migrator.LoadAllMigrationTypes();
        var loaded     = migrator.LoadAllMigrations();

        Assert.That(migrations.Count, Is.GreaterThanOrEqualTo(1));
    }

}