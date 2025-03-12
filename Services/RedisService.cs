using StackExchange.Redis;

namespace Valuator.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public void SaveRank(string id, double rank)
        {
            _db.StringSet($"rank:{id}", rank);
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

        public void SaveSimilarity(string id, double similarity)
        {
            _db.StringSet($"similarity:{id}", similarity);
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

        public bool IsDuplicateText(string text)
        {
            return _db.SetContains("processed_texts", text);
        }

        public void SaveProcessedText(string text)
        {
            _db.SetAdd("processed_texts", text);
        }
        public bool IsConnected()
        {
            try
            {
                return _db.Multiplexer.IsConnected;
            }
            catch
            {
                return false;
            }
        }
    }
}
