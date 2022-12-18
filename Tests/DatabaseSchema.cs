using Driver;

namespace Tests;

public class DatabaseSchemaTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestDbInfoCmd()
    {
        var results = await Database.Info();

        Assert.IsNotNull(results);
        Assert.IsTrue(results!.Tables.Count > 0);
    }

    [Test]
    public async Task TestDbTableCmd()
    {
        var results = await Database.GetTableInfo("users");

        Assert.IsNotNull(results);
        Assert.IsTrue(results!.Fields.Count > 0);
    }

    [Test]
    public async Task TestDbSchema()
    {
        var schema = await Database.GetSchema();

        Assert.IsNotNull(schema);
    }
}