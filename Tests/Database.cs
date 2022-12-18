using Driver;
using Driver.Models;
using Driver.Models.Attributes;
using Driver.Models.Types;
using Newtonsoft.Json;

namespace Tests;

public class DatabaseTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestDatabaseInitialization()
    {
        Database.Initialize();
        await Database.Connect();

        Assert.IsTrue(Database.IsConnected());
    }

    public class TestEntry : SurrealModel<TestEntry>
    {
        [JsonProperty("value")]
        public string Value { get; set; } = null!;
    }

    [Test]
    public async Task TestDatabaseQuery()
    {
        Database.Initialize();
        await Database.Connect();

        var insertResult = await Database.Query("INSERT into test { value: 'send help'} ;");
        Assert.IsNotNull(insertResult);

        dynamic testItem = insertResult!.First()!;
        Assert.IsNotNull(testItem);
        // Assert.AreEqual("send help", testItem!.value);

        var result = await Database.Query<TestEntry>("SELECT * FROM test;");
        Assert.IsNotNull(result);
        var nextItem = result!.First();
        Assert.IsNotNull(nextItem);
        Assert.AreEqual("send help", nextItem!.Value);
    }

}