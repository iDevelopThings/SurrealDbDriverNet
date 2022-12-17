using System.Reflection;

namespace Driver.Query.Grammar;

public abstract class ExpOperatorEnum<TEnum> where TEnum : ExpOperatorEnum<TEnum>
{
    public string       Name   { get; private set; }
    public List<string> Values { get; private set; }

    protected ExpOperatorEnum(string name, List<string> values)
        => (Name, Values) = (name, values);

    public string Default() => Values[0];

    public override string ToString() => Default();

    public static IEnumerable<T> GetAll<T>() where T : ExpOperatorEnum<TEnum> =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
           .Select(f => f.GetValue(null))
           .Cast<T>();

    public static T? Parse<T>(string value) where T : ExpOperatorEnum<TEnum>
    {
        return GetAll<T>().FirstOrDefault(x => x.Name == value);
    }

}

public class ExpressionOperator : ExpOperatorEnum<ExpressionOperator>
{
    public static readonly ExpressionOperator Equal       = new("Equals", new() {"=", "==", "IS"});
    public static readonly ExpressionOperator DoesntEqual = new("DoesntEqual", new() {"!=", "IS NOT"});
    public static readonly ExpressionOperator Or          = new("Or", new() {"||", "OR"});
    public static readonly ExpressionOperator And         = new("And", new() {"&&", "AND"});
    public static readonly ExpressionOperator ContainsAny = new("ContainsAny", new() {"CONTAINSANY", "⊃"});
    public static readonly ExpressionOperator Lte         = new("LessThanOrEqual", new() {"<="});
    public static readonly ExpressionOperator Lt          = new("LessThan", new() {"<"});
    public static readonly ExpressionOperator Gte         = new("GreaterThanOrEqual", new() {">="});
    public static readonly ExpressionOperator Gt          = new("GreaterThan", new() {">"});

    public ExpressionOperator(string name, List<string> values) : base(name, values)
    {
    }
}