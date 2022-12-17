using IocContainer;
using Reflection;

namespace Driver.Models;

public class ModelDriver
{
    public static Dictionary<Type, ISurrealModel> Models = new Dictionary<Type, ISurrealModel>();

    /// <summary>
    /// Register all models found within the project
    /// This will add them to the container, and our static dictionary
    /// </summary>
    public static void RegisterAllModels()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
           .SelectMany(s => s.GetTypes())
           .Where(p => typeof(ISurrealModel).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract && p != typeof(SurrealModel<>))
           .ToList();

        foreach (var type in types) {
            RegisterModel(type);
        }
    }

    /// <summary>
    /// Registers the model with the container and adds it to our static dictionary
    /// </summary>
    /// <param name="type"></param>
    public static void RegisterModel(Type type)
    {
        var descriptor = Container.RegisterTransient(type);
        descriptor.AddTag("Model");

        Models.Add(type, (ISurrealModel) Container.Resolve(type));
    }

}