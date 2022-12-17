using System.Reflection;
using Driver.Models.Attributes;
using Driver.Models.Types;
using Newtonsoft.Json;

namespace Driver.Models.Utils;

public abstract class ModelFieldTypeEnum<TEnum> where TEnum : ModelFieldTypeEnum<TEnum>
{
    public string Key   { get; private set; }
    public string Value { get; private set; }

    public bool HasParameters { get; private set; }

    protected ModelFieldTypeEnum(string key, string value, bool hasParameters) => (Key, Value, HasParameters) = (key, value, hasParameters);

    public override string ToString() => Value;

    public static IEnumerable<T> GetAll<T>() where T : ModelFieldTypeEnum<TEnum> =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
           .Select(f => f.GetValue(null))
           .Cast<T>();

    public static T? Parse<T>(string value) where T : ModelFieldTypeEnum<TEnum>
    {
        return GetAll<T>().FirstOrDefault(x => x.Value == value);
    }
}

public class ModelFieldTypeInfo
{

    public ModelFieldTypes Kind { get; set; }
    public Type?           Type { get; set; }

    public ModelFieldTypeInfo? SubKind { get; set; }
    public Type?               SubType { get; set; }

    public ModelFieldTypeInfo(ModelFieldTypes kind, Type type)
    {
        Kind = kind;
        Type = type;
    }

    public string ToSqlString()
    {
        var str = "";

        str += Kind.Value;

        return str;
        // if (Kind.HasParameters && TypeParams.Count > 0) {
        // str += "(";
        // str += string.Join(", ", TypeParams);
        // str += ")";
        // } /*else if (Type.Kind == ModelFieldTypes.Array) {
        //         str += "()";
        //     }*/
    }
}

[JsonConverter(typeof(ModelFieldTypesConverter))]
public class ModelFieldTypes : ModelFieldTypeEnum<ModelFieldTypes>
{
    public static readonly ModelFieldTypes Any           = new("ANY", "any");
    public static readonly ModelFieldTypes Array         = new("ARRAY", "array");
    public static readonly ModelFieldTypes Bool          = new("BOOL", "bool");
    public static readonly ModelFieldTypes Datetime      = new("DATETIME", "datetime");
    public static readonly ModelFieldTypes Decimal       = new("DECIMAL", "decimal");
    public static readonly ModelFieldTypes Duration      = new("DURATION", "duration");
    public static readonly ModelFieldTypes Float         = new("FLOAT", "float");
    public static readonly ModelFieldTypes Int           = new("INT", "int");
    public static readonly ModelFieldTypes Number        = new("NUMBER", "number");
    public static readonly ModelFieldTypes Object        = new("OBJECT", "object");
    public static readonly ModelFieldTypes String        = new("STRING", "string");
    public static readonly ModelFieldTypes Record        = new("RECORD", "record", true);
    public static readonly ModelFieldTypes Geometry      = new("GEOMETRY", "geometry", true);
    public static readonly ModelFieldTypes GeometryPoint = new("GEOMETRY__POINT", "geometry(point)", true);

    public static readonly ModelFieldTypes InternalNestedType = new("INTERNAL_NESTED_TYPE", "__", false);


    public ModelFieldTypes(string key, string value, bool hasParameters = false) : base(key, value, hasParameters)
    {
    }


    public static ModelFieldTypeInfo? From(Type type)
    {
        GetNonNullableType(type, out var isNullable);

        // check if type is an array type
        if (IsArrayType(type)) {
            var   genArgs = type.GetGenericArguments();
            Type? subType = null;
            if (genArgs.Length > 0) {
                subType = genArgs[0];
            }

            return new ModelFieldTypeInfo(Array, type) {
                SubType = subType,
                SubKind = subType == null ? null : From(subType)
            };
        }

        // Bool
        if (type.IsAssignableFrom(typeof(bool))) {
            return new ModelFieldTypeInfo(Bool, type);
        }

        // Datetime
        if (type.IsAssignableFrom(typeof(DateTime))) {
            return new ModelFieldTypeInfo(Datetime, type);
        }

        // Decimal
        if (type.IsAssignableFrom(typeof(decimal))) {
            return new ModelFieldTypeInfo(Decimal, type);
        }

        // Duration
        if (type.IsAssignableFrom(typeof(TimeSpan))) {
            return new ModelFieldTypeInfo(Duration, type);
        }

        // Float
        if (type.IsAssignableFrom(typeof(float))) {
            return new ModelFieldTypeInfo(Float, type);
        }

        // Int
        if (IsIntType(type)) {
            return new ModelFieldTypeInfo(Int, type);
        }

        // Number
        if (type.IsAssignableFrom(typeof(double))) {
            return new ModelFieldTypeInfo(Number, type);
        }

        // String
        if (IsStringType(type)) {
            return new ModelFieldTypeInfo(String, type);
        }

        // Record
        if (type.IsAssignableFrom(typeof(SurrealModel<>))) {
            return new ModelFieldTypeInfo(Record, type);
        }

        // Geometry
        if (type.IsAssignableFrom(typeof(GeometryPoint))) {
            return new ModelFieldTypeInfo(GeometryPoint, type);
        }

        if (IsNestedType(type)) {
            return new ModelFieldTypeInfo(Object, type);
        }

        // Object
        if ((type.IsClass && !type.IsPrimitive && !type.IsArray && !type.IsGenericType)) {
            return new ModelFieldTypeInfo(Object, type);
        }

        return null;
    }

    public static void GetNonNullableType(Type type, out object isNullable)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            isNullable = true;
            type       = type.GetGenericArguments()[0];
        } else {
            isNullable = false;
        }
    }

    public static bool IsArrayType(Type type)
    {
        return type.IsGenericType &&
               (
                   type.GetGenericTypeDefinition() == typeof(IList<>) ||
                   type.GetGenericTypeDefinition() == typeof(List<>) ||
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                   type.GetGenericTypeDefinition() == typeof(ICollection<>)
               );
    }

    public static bool IsEnumType(Type type)
    {
        // TODO: Library needs a way to register custom mappings?
        return type.IsEnum ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IsEnum)
            // type.IsAssignableTo(typeof(IStringEnum)) ||
            // type.IsAssignableTo(typeof(IIntEnum));
            ;
    }

    private static bool BaseNestedTypeChecks(Type type)
    {
        return type is {IsPrimitive: false, IsArray: false} &&
               (type.IsClass || type.IsValueType) &&
               !IsEnumType(type) &&
               !type.IsInterface &&
               !type.IsPointer &&
               !type.IsAbstract &&
               !type.IsSealed &&
               !type.Namespace!.StartsWith("System.") &&
               !IsInternallyNestedType(type);
    }

    public static bool IsInternallyNestedType(Type type)
    {
        if (type.IsAssignableFrom(typeof(GeometryPoint))) {
            return true;
        }

        return false;
    }

    public static bool IsNestedType(Type type) => BaseNestedTypeChecks(type);

    public static bool IsNestedPropertyType(PropertyInfo type)
    {
        return IsNestedType(type.PropertyType) ||
               (
                   type.GetCustomAttribute<NestedAttribute>() != null
               );
    }

    public static bool IsStringType(Type type)
    {
        return type.IsAssignableFrom(typeof(string)) ||
               type.IsAssignableFrom(typeof(char)) ||
               type.IsAssignableFrom(typeof(Guid)) ||
               type.IsAssignableFrom(typeof(Uri)) ||
               type.IsAssignableFrom(typeof(Version)) /* ||
               type.IsAssignableTo(typeof(IStringEnum))*/;
    }

    public static bool IsIntType(Type type)
    {
        return type.IsAssignableFrom(typeof(int)) ||
               type.IsAssignableFrom(typeof(long)) ||
               type.IsAssignableFrom(typeof(short)) ||
               type.IsAssignableFrom(typeof(byte)) ||
               type.IsAssignableFrom(typeof(uint)) ||
               type.IsAssignableFrom(typeof(ulong)) ||
               type.IsAssignableFrom(typeof(ushort)) ||
               type.IsAssignableFrom(typeof(sbyte)) ||
               /*type.IsAssignableTo(typeof(IIntEnum)) ||*/
               type.IsAssignableFrom(typeof(Enum));
    }
}

public class ModelFieldTypesConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(((ModelFieldTypes) value!).Value);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var rawVal = reader.Value!.ToString()!;
        var result = ModelFieldTypes.Parse<ModelFieldTypes>(rawVal);

        return result!;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ModelFieldTypes);
    }
}