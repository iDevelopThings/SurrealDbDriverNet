using Driver.Models;

namespace Tests;

public class ModelBaseTests
{
    [SetUp]
    public void Setup()
    {
    }

    private class TestRegistrationModel : SurrealModel<TestRegistrationModel>
    {
        
    }
    
    [Test]
    public async Task TestModelsAreRegisteredWithContainer()
    {
        var models = ModelDriver.Models;
        Assert.GreaterOrEqual(models.Count, 1);
        
        var model = models.FirstOrDefault(m => m.Key.Name == nameof(TestRegistrationModel));
        Assert.NotNull(model);
    }

}