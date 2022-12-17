using Driver.Models.Types;

namespace Driver.Query.Grammar.Clauses;

public class GeoDistanceClause : QueryProjectMethod
{
    public string?        Field       { get; set; }
    public GeometryPoint? FromPoint   { get; set; }
    public double         MaxDistance { get; set; }

    public GeoDistanceClause(string field)
    {
        Field = field;
    }

    public GeoDistanceClause From(GeometryPoint point)
    {
        FromPoint = point;
        return this;
    }

    public GeoDistanceClause Max(double distance)
    {
        MaxDistance = distance;
        return this;
    }

    public string BuildFunction()
    {
        var fromPoint = $"({FromPoint!.Longitude}, {FromPoint!.Latitude})";
        return $"(geo::distance({fromPoint}, {Field}) * {GeometryPoint.MetersToMiles})";
    }

}