using Newtonsoft.Json;

namespace Driver.Models.Types;

public class GeometryPoint
{
    [JsonProperty("type")]
    public string Type => "Point";

    [JsonProperty("coordinates")]
    public List<double> Coordinates { get; set; }

    public GeometryPoint(double lat, double lng)
    {
        Coordinates = new List<double> {lat, lng};
    }

    [JsonIgnore]
    public double Latitude => Coordinates[0];

    [JsonIgnore]
    public double Longitude => Coordinates[1];

    public const double MetersToMiles = 0.000621371192;
    
    public double DistanceTo(GeometryPoint target)
    {
        if (Coordinates.Count != 2 || target.Coordinates.Count != 2) {
            return -1;
        }

        var toLatitude  = target.Latitude;
        var toLongitude = target.Longitude;

        var fromLatitude  = this.Latitude;
        var fromLongitude = this.Longitude;

        double x = 69.1 * (toLatitude - fromLatitude);
        double y = 69.1 * (toLongitude - fromLongitude) * Math.Cos(fromLatitude / 57.3);

        var dist = Math.Sqrt(x * x + y * y);

        return dist * MetersToMiles; // returns miles
    }
}