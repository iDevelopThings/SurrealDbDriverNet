using Driver.Models;
using Driver.Models.Attributes;
using Newtonsoft.Json;

namespace Tests;

public class ModelQueryTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Model("test_users")]
    public class TestUser : SurrealModel<TestUser>
    {
        [JsonProperty("username")]
        public string Username { get; set; } = null!;
    }

    [Test]
    public async Task TestModelQuery()
    {
        var testUserOne = await TestUser.Create(new() {Username = "Bob"});

        var testUsers = await TestUser.Query().Where("id", testUserOne.Id).Get();

        Assert.That(testUsers.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task TestWhereWithExpression()
    {
        var testUsers = await TestUser.Query().Where(x => x.Username, "Bob").Get();

        Assert.That(testUsers.Count, Is.EqualTo(1));
    }

}