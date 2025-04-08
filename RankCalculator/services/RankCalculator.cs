using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Valuator.Services;

namespace RankCalculator.services
{
    public class RankCalculator : IHostedService
    {
        private readonly RedisService _redisService;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RankCalculator(RedisService redisService, IConnectionFactory connectionFactory)
        {
            _redisService = redisService;
            _connectionFactory = connectionFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Инициализация RabbitMQ соединения
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "rank_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);


            // Прослушивание очереди RabbitMQ
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var id = Encoding.UTF8.GetString(body);

                if (id != null)
                {
                    await ProcessTextAsync(id);
                }
            };
            _channel.BasicConsume(queue: "rank_tasks", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Close();
            _connection.Close();
            return Task.CompletedTask;
        }

        private async Task ProcessTextAsync(string id)
        {
            try
            {
                Console.WriteLine("[CONSOLE] Начало обработки...");

                string textKey = "TEXT-" + id;

                string? UserText = await _redisService.GetTextAsync(textKey);

                if (UserText == null)
                {
                    Console.WriteLine($"[CONSOLE] Текст с ключом: {textKey} не найден!");
                    return;
                }
                Console.WriteLine($"[CONSOLE] Текст с ключом: {textKey} найден!");

                double rank = CalculateRank(UserText);
                string textHash = GetTextHash(UserText);

                double similarity = await _redisService.IsDuplicateTextAsync(UserText) ? 1.0 : 0.0;

                string rankKey = "RANK-" + id;
                string similarityKey = "SIMILARITY-" + id;

                await _redisService.SaveTextAsync(textKey, textHash);
                await _redisService.SaveRankAsync(rankKey, rank);
                await _redisService.SaveSimilarityAsync(similarityKey, similarity);
                await _redisService.SaveProcessedTextAsync(UserText);

                Console.WriteLine($"[CONSOLE] Завершено вычисление для текста с ID: {id} | Rank: {rank} | Similarity: {similarity}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONSOLE] Ошибка вычисления rank и similarity: {ex.Message}");
            }

        }

        private double CalculateRank(string text)
        {
            int totalChars = text.Length;
            int alphaChars = text.Count(char.IsLetter);
            int nonAlphaChars = totalChars - alphaChars;

            return totalChars > 0 ? (double)nonAlphaChars / totalChars : 0.0;
        }

        private string GetTextHash(string text)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}