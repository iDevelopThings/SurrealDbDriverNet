using Driver.Models;
using Driver.Models.Attributes;
using Driver.Models.Types;
using Newtonsoft.Json;

namespace Tests;

public class ThingTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Model("users")]
    public class User : SurrealModel<User>
    {
    }

    [Test]
    public void TestThingValues()
    {
        var thing1 = Thing.From("test:1234");
        var thing2 = Thing.From("test", "1234");
        var thing3 = Thing.From<int>("test", 1234);
        var thing4 = Thing.From<User>("1234");
        var thing5 = Thing.From<User, int>(1234);


        Assert.That(thing1.ToString(), Is.EqualTo("test:1234"));
        Assert.That(thing2.ToString(), Is.EqualTo("test:1234"));
        Assert.That(thing3.ToString(), Is.EqualTo("test:1234"));
        Assert.That(thing4.ToString(), Is.EqualTo("users:1234"));
        Assert.That(thing5.ToString(), Is.EqualTo("users:1234"));

        Assert.That(thing1.Table.ToString(), Is.EqualTo("test"));
        Assert.That(thing1.Key.ToString(), Is.EqualTo("1234"));

        Assert.That(thing2.Table.ToString(), Is.EqualTo("test"));
        Assert.That(thing2.Key.ToString(), Is.EqualTo("1234"));

        Assert.That(thing3.Table.ToString(), Is.EqualTo("test"));
        Assert.That(thing3.Key.ToString(), Is.EqualTo("1234"));

        Assert.That(thing4.Table.ToString(), Is.EqualTo("users"));
        Assert.That(thing4.Key.ToString(), Is.EqualTo("1234"));

        Assert.That(thing5.Table.ToString(), Is.EqualTo("users"));
        Assert.That(thing5.Key.ToString(), Is.EqualTo("1234"));
    }


    private struct TestStruct
    {
        [JsonProperty("id")]
        public Thing Id { get; set; }
    }

    [Test]
    public void TestThingJsonHandling()
    {
        var obj = new TestStruct() {Id = Thing.From("test:1234")};

        var serialized   = JsonConvert.SerializeObject(obj);
        var deserialized = JsonConvert.DeserializeObject<TestStruct>(serialized);

        Assert.That(deserialized.Id, Is.EqualTo(obj.Id));
        Assert.That(deserialized.Id.ToString(), Is.EqualTo(obj.Id.ToString()));

        Assert.That(deserialized.Id.Table.ToString(), Is.EqualTo(obj.Id.Table.ToString()));
        Assert.That(deserialized.Id.Key.ToString(), Is.EqualTo(obj.Id.Key.ToString()));

        Assert.That(serialized, Is.EqualTo(@"{""id"":""test:1234""}"));
    }
}