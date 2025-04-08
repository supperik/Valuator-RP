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

        public async Task SaveRankAsync(string id, double rank)
        {
            await _db.StringSetAsync($"rank:{id}", rank);
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

        public async Task SaveSimilarityAsync(string id, double similarity)
        {
            await _db.StringSetAsync($"similarity:{id}", similarity);
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

        public async Task<bool> IsDuplicateTextAsync(string text)
        {
            return await _db.SetContainsAsync("processed_texts", text);
        }

        public void SaveProcessedText(string textHash)
        {
            _db.SetAdd("processed_texts", textHash);
        }

        public async Task SaveProcessedTextAsync(string textHash)
        {
            await _db.SetAddAsync("processed_texts", textHash);
        }

        public void SaveText(string textKey, string text)
        {
            _db.StringSet(textKey, text);
        }

        public async Task SaveTextAsync(string textKey, string text)
        {
            await _db.StringSetAsync(textKey, text);
        }

        public string GetText(string textKey)
        {
            return _db.StringGet(textKey);
        }

        public async Task<string> GetTextAsync(string textKey)
        {
            return await _db.StringGetAsync(textKey);
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
