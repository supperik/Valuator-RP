namespace Valuator.Sharding;

public enum Region
{
    RU,
    EU,
    ASIA
}

public static class CountryRegionMapper
{
    private static readonly Dictionary<string, Region> _map = new()
    {
        { "Russia", Region.RU },
        { "France", Region.EU },
        { "Germany", Region.EU },
        { "UAE", Region.ASIA },
        { "India", Region.ASIA }
    };

    public static Region GetRegion(string country)
    {
        return _map.TryGetValue(country, out var region) ? region : throw new Exception($"Unknown country: {country}");
    }
}
