# SurrealDb Driver

An un-official driver for the SurrealDb database.

Example usage:

```csharp
using SurrealDb.Driver;

Database.Configure(config =>
{
    config.Address      = "http://127.0.0.1:8082";
    config.DatabaseName = "test";
    config.Namespace    = "test";
    config.AuthUsername = "root";
    config.AuthPassword = "root";
});
Database.Initialize();
await Database.Connect();
```

# Create a model

```csharp
[Model("users")]
public class User : SurrealModel<User>
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```

# Run queries

```csharp
using SurrealDb.Driver;

var result = await Database.Query<User>("SELECT * FROM users;");

// When expecting one user:
var user = result!.First();
// When expecting multiple users:
var users = result!.Get();
```

# Using the model for queries
This is a work in progress, but it will be more like a laravel query builder instance :)

```csharp
using SurrealDb.Driver;

// Returns multiple users where name = john
User.Query().Where("name", "John").Get(); // = List<User>
// Returns first user where name = john
User.Query().Where("name", "John").First(); // = User

var john = await User.Create(new(){ Name = "John" });

```

