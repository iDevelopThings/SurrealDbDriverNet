namespace Reflection;

public class TypeChecker
{
    private static Type TypeValue(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            return Nullable.GetUnderlyingType(type)!;
        }

        return type;
    }

    public static bool IsInt(Type type) => TypeValue(type) == typeof(int);

    public static bool IsLong(Type type) => TypeValue(type) == typeof(long);

    public static bool IsString(Type type) => TypeValue(type) == typeof(string);

    public static bool IsBool(Type type) => TypeValue(type) == typeof(bool);

    public static bool IsDateTime(Type type) => TypeValue(type) == typeof(DateTime);

    public static bool IsDouble(Type type) => TypeValue(type) == typeof(double);

    public static bool IsDecimal(Type type) => TypeValue(type) == typeof(decimal);

    public static bool IsGuid(Type type) => TypeValue(type) == typeof(Guid);

    public static bool IsEnum(Type type) => TypeValue(type).IsEnum;

    public static bool IsList(Type type) => TypeValue(type).IsGenericType && TypeValue(type).GetGenericTypeDefinition() == typeof(List<>);

    public static bool IsDictionary(Type type) => TypeValue(type).IsGenericType && TypeValue(type).GetGenericTypeDefinition() == typeof(Dictionary<,>);

    public static bool IsNullable(Type type) => TypeValue(type).IsGenericType && TypeValue(type).GetGenericTypeDefinition() == typeof(Nullable<>);

    public static bool IsClass(Type type) => TypeValue(type).IsClass;

    public static bool IsStruct(Type type) => TypeValue(type).IsValueType && !IsNullable(type);

    public static bool IsInterface(Type type) => TypeValue(type).IsInterface;

    public static bool IsPrimitive(Type type) => TypeValue(type).IsPrimitive;

    public static bool IsArray(Type type) => TypeValue(type).IsArray;

    public static bool IsValueType(Type type) => TypeValue(type).IsValueType;

    public static bool IsGenericType(Type type) => TypeValue(type).IsGenericType;

}

public class ObjectTypeChecker
{
    private readonly Type type;

    public ObjectTypeChecker(Type type)
    {
        this.type = type;
    }

    public bool IsInt() => TypeChecker.IsInt(type);

    public bool IsLong() => TypeChecker.IsLong(type);

    public bool IsString() => TypeChecker.IsString(type);

    public bool IsBool() => TypeChecker.IsBool(type);

    public bool IsDateTime() => TypeChecker.IsDateTime(type);

    public bool IsDouble() => TypeChecker.IsDouble(type);

    public bool IsDecimal() => TypeChecker.IsDecimal(type);

    public bool IsGuid() => TypeChecker.IsGuid(type);

    public bool IsEnum() => TypeChecker.IsEnum(type);

    public bool IsList() => TypeChecker.IsList(type);

    public bool IsDictionary() => TypeChecker.IsDictionary(type);

    public bool IsNullable() => TypeChecker.IsNullable(type);

    public bool IsClass() => TypeChecker.IsClass(type);

    public bool IsStruct() => TypeChecker.IsStruct(type);

    public bool IsInterface() => TypeChecker.IsInterface(type);

    public bool IsPrimitive() => TypeChecker.IsPrimitive(type);

    public bool IsArray() => TypeChecker.IsArray(type);

    public bool IsValueType() => TypeChecker.IsValueType(type);

    public bool IsGenericType() => TypeChecker.IsGenericType(type);


}