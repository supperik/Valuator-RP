using StackExchange.Redis;
using Valuator.Services;

namespace Valuator.Sharding;

public class RedisShardManager
{
    private readonly Dictionary<string, ConnectionMultiplexer> _connections = new();
    private readonly IDatabase _mainDb;

    public RedisShardManager()
    {
        var defaultConfig = new Dictionary<string, string>
        {
            { "DB_MAIN", "localhost:5000" },
            { "DB_RU", "localhost:5001" },
            { "DB_EU", "localhost:5002" },
            { "DB_ASIA", "localhost:5003" }
        };

        // Получаем строку подключения для MAIN
        string dbMain = Environment.GetEnvironmentVariable("DB_MAIN") ?? defaultConfig["DB_MAIN"];
        _connections["MAIN"] = ConnectionMultiplexer.Connect(dbMain);
        _mainDb = _connections["MAIN"].GetDatabase();

        // Обрабатываем регионы
        foreach (var region in new[] { "RU", "EU", "ASIA" })
        {
            string envKey = $"DB_{region}";
            string connStr = Environment.GetEnvironmentVariable(envKey) ?? defaultConfig[envKey];

            _connections[region] = ConnectionMultiplexer.Connect(connStr);
        }
    }

    public void SetShardMap(string id, string region)
    {
        _mainDb.StringSet($"SHARDMAP-{id}", region);
    }

    public string? GetRegion(string id)
    {
        var region = _mainDb.StringGet($"SHARDMAP-{id}");
        return region.HasValue ? region.ToString() : null;
    }

    public IDatabase GetRegionDb(string region)
    {
        if (!_connections.ContainsKey(region))
            throw new Exception($"No Redis connection for region: {region}");

        return _connections[region].GetDatabase();
    }

    public RedisService GetRedisServiceByRegion(string region)
    {
        if (!_connections.ContainsKey(region))
            throw new Exception($"No Redis connection for region: {region}");

        return new RedisService(_connections[region]);
    }

    public RedisService GetRedisServiceByCountry(string country)
    {
        Region region = CountryRegionMapper.GetRegion(country);
        return GetRedisServiceByRegion(region.ToString());
    }

    public void LogLookup(string id, string region, ILogger? logger = null)
    {
        string message = $"LOOKUP: {id}, {region}";
        if (logger != null)
            logger.LogInformation(message);
        else
            Console.WriteLine(message);
    }
}
