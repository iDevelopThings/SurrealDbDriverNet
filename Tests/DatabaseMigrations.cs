using Driver;
using Driver.Migrations;
using Driver.Models;
using Driver.Models.Attributes;
using Driver.Models.Types;
using Newtonsoft.Json;

namespace Tests;

public class DatabaseMigrationsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(MigrationGenerator.MigrationsPath)) {
            Directory.Delete(MigrationGenerator.MigrationsPath, true);
        }
    }

    [Test]
    public async Task TestCreatingMigrations()
    {
        MigrationGenerator generator   = new MigrationGenerator();
        var                didGenerate = generator.Build();

        Assert.IsTrue(didGenerate);
    }

}