namespace Driver.Query.Grammar;

public class QueryParameter
{
    public string VariableName { get; set; } = null!;
    public object Value        { get; set; } = null!;
}