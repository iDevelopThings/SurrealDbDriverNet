namespace Driver.Query.Grammar;

public enum OrderDirection
{
    Ascending,
    Descending,
}

public static class OrderDirectionExt
{
    public static string ToDescription(this OrderDirection direction)
    {
        return direction switch {
            OrderDirection.Ascending  => "ASC",
            OrderDirection.Descending => "DESC",
            _                         => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}

public class QueryOrder
{
    public string? Column { get; set; }

    public OrderDirection Direction { get; set; }
}
