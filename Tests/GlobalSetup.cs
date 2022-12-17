using Driver;

namespace Tests;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public async Task SetUp()
    {
        Database.Configure(config =>
        {
            config.Address      = "http://127.0.0.1:8082";
            config.DatabaseName = "test";
            config.Namespace    = "test";
            config.AuthUsername = "root";
            config.AuthPassword = "root";
        });
        Database.Initialize();

        if (!Database.IsConnected()) {
            await Database.Connect();
        }
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (Database.IsConnected()) {
            await Database.Disconnect();
        }
    }
}