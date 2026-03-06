namespace ParameterValidation.Domains;

public sealed class Coordinate
{
    public double Latitude { get; }
    public double Longitude { get; }

    private Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Coordinate Create(double latitude, double longitude)
        => new(latitude, longitude);
}
