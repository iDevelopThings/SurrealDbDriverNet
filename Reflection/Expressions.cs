using System.Linq.Expressions;
using System.Reflection;

namespace Reflection;

public static class Expressions
{
    private static readonly string ExpressionCannotBeNullMessage = "The expression cannot be null.";
    private const           string InvalidExpressionMessage      = "Invalid expression.";

    public static string GetMemberName<T>(this T instance, Expression<Func<T, object>> expression)
    {
        return GetMemberName(expression.Body);
    }

    public static List<string> GetMemberNames<T>(this T instance, params Expression<Func<T, object>>[] expressions)
    {
        List<string> memberNames = new List<string>();
        foreach (var cExpression in expressions) {
            memberNames.Add(GetMemberName(cExpression.Body));
        }

        return memberNames;
    }

    public static string GetMemberName<T>(this T instance, Expression<Action<T>> expression)
    {
        return GetMemberName(expression.Body);
    }

    private static string GetMemberName(Expression expression)
    {
        if (expression == null) {
            throw new ArgumentException(ExpressionCannotBeNullMessage);
        }

        if (expression is MemberExpression memberExpression) {
            // Reference type property or field
            return memberExpression.Member.Name;
        }

        if (expression is MethodCallExpression methodCallExpression) {
            // Reference type method
            return methodCallExpression.Method.Name;
        }

        if (expression is UnaryExpression unaryExpression) {
            // Property, field of method returning value type
            return GetMemberName(unaryExpression);
        }

        throw new ArgumentException(InvalidExpressionMessage);
    }

    private static string GetMemberName(UnaryExpression unaryExpression)
    {
        if (unaryExpression.Operand is MethodCallExpression) {
            var methodExpression = (MethodCallExpression) unaryExpression.Operand;
            return methodExpression.Method.Name;
        }

        return ((MemberExpression) unaryExpression.Operand).Member.Name;
    }

    public static ExpressionInfo<T> GetMemberInfo<T>(
        this Expression<Func<T, object>> expression,
        Func<MemberInfo, string>         paramNameMapper
    ) => GetMember<T>(expression, paramNameMapper);

    public static ExpressionInfo<T> GetMember<T>(
        Expression<Func<T, object>> baseExpression,
        Func<MemberInfo, string>    paramNameMapper
    )
    {
        var expression = baseExpression.Body;

        if (expression == null) {
            throw new ArgumentException(ExpressionCannotBeNullMessage);
        }

        if (expression is MemberExpression memberExpression) {
            // Reference type property or field
            return new ExpressionInfo<T>(baseExpression, memberExpression.Member, paramNameMapper);
        }

        if (expression is MethodCallExpression methodCallExpression) {
            // Reference type method
            return new ExpressionInfo<T>(baseExpression, methodCallExpression.Method, paramNameMapper);
            // return methodCallExpression.Method;
        }

        if (expression is UnaryExpression unaryExpression) {
            // Property, field of method returning value type
            return GetMember<T>(baseExpression, unaryExpression, paramNameMapper);
        }

        throw new ArgumentException(InvalidExpressionMessage);
    }

    private static ExpressionInfo<T> GetMember<T>(Expression<Func<T, object>> baseExpression, UnaryExpression unaryExpression, Func<MemberInfo, string> paramNameMapper)
    {
        if (unaryExpression.Operand is MethodCallExpression) {
            var methodExpression = (MethodCallExpression) unaryExpression.Operand;
            return new ExpressionInfo<T>(baseExpression, methodExpression.Method, paramNameMapper);
        }

        return new ExpressionInfo<T>(baseExpression, ((MemberExpression) unaryExpression.Operand).Member, paramNameMapper);
    }

    public static string GetMemberPath<T>(this T instance, Expression<Func<T, object>> expression)
    {
        return GetMemberPath<T>(expression);
    }

    public static string GetMemberPath<T>(Expression<Func<T, object>> expression, Func<MemberInfo, string>? paramNameMapper = null)
    {
        var path = new List<string>();

        expression = expression ?? throw new ArgumentException(ExpressionCannotBeNullMessage);

        var memberExpression = expression.Body as MemberExpression;
        if (expression.Body is MemberExpression) {
            if (expression.Body is UnaryExpression unaryExpression) {
                memberExpression = (unaryExpression.Operand as MemberExpression)!;
            }

            if (expression.Body is MethodCallExpression methodCall) {
                path.Add(methodCall.Method.Name);
                memberExpression = (methodCall.Object as MemberExpression)!;
            }

            if (memberExpression == null) {
                throw new ArgumentException(InvalidExpressionMessage);
            }
        } else if (expression.Body is MethodCallExpression methodCall) {
            path.Add(methodCall.Method.Name);
            memberExpression = (methodCall.Object as MemberExpression)!;
        } else {
            throw new ArgumentException(InvalidExpressionMessage);
        }

        while (memberExpression != null) {
            paramNameMapper ??= type => type.Name;
            path.Add(paramNameMapper(memberExpression.Member));
            // path.Add(memberExpression.Member.Name);
            if (memberExpression.Expression is MemberExpression parentMemberExpression) {
                memberExpression = parentMemberExpression;
            } else if (memberExpression.Expression is MethodCallExpression mCallExpr) {
                memberExpression = mCallExpr.Object as MemberExpression;
            } else {
                memberExpression = null;
            }
        }

        path.Reverse();

        return string.Join(".", path);
    }
}

public class ExpressionInfo<TObjType>
{
    public Expression<Func<TObjType, object>> Expression { get; set; }

    public string Path { get; set; }
    public string Name { get; set; }

    public bool IsMethod   { get; set; }
    public bool IsProperty { get; set; }

    public Type Type { get; set; }

    public ExpressionInfo(Expression<Func<TObjType, object>> expression, MemberInfo member, Func<MemberInfo, string> paramNameMapper)
    {
        paramNameMapper ??= type => type.Name;
        
        Expression = expression;
        Path       = Expressions.GetMemberPath(expression, paramNameMapper);
        // Name       = member.Name;
        Name       = paramNameMapper(member); // member.Name;
        IsMethod   = member is MethodInfo;
        IsProperty = member is PropertyInfo;

        if (member is PropertyInfo propertyInfo) {
            Type = propertyInfo.PropertyType;
        } else if (member is MethodInfo methodInfo) {
            Type = methodInfo.ReturnType;
        } else if (member is FieldInfo fieldInfo) {
            Type = fieldInfo.FieldType;
        } else {
            throw new ArgumentException("Invalid member type.");
        }
    }

}