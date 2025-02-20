using StackExchange.Redis;
using System;

namespace Valuator.Services;

public class RedisService
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService()
    {
        try
        {
            _redis = ConnectionMultiplexer.Connect("localhost:6379");
            _db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения к Redis: {ex.Message}");
            throw;
        }
    }

    public bool IsConnected()
    {
        return _redis != null && _redis.IsConnected;
    }

    public double? GetRank(string id)
    {
        string value = _db.StringGet($"rank:{id}");
        if (value != null)
        {
            return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            return 0.0;
        }
    }

    public double? GetSimilarity(string id)
    {
        string value = _db.StringGet($"similarity:{id}");
        if (value != null)
        {
            return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            return 0.0;
        }
    }
}
