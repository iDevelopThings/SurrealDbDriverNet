using System.Reflection;

namespace Reflection;

public static class TypeExtensions
{
    public static IEnumerable<Type> GetAllTypes(this Type type)
    {
        var typeInfo = type.GetTypeInfo();
        var allTypes = GetAllImplementedTypes(type).Concat(typeInfo.ImplementedInterfaces);
        return allTypes;
    }

    private static IEnumerable<Type> GetAllImplementedTypes(Type type)
    {
        yield return type;
        var typeInfo = type.GetTypeInfo();
        var baseType = typeInfo.BaseType;
        if (baseType != null)
        {
            foreach (var foundType in GetAllImplementedTypes(baseType))
            {
                yield return foundType;
            }
        }
    }
}