using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Valuator.Services;
using Valuator.Pages;

using Newtonsoft.Json;
using System.Threading.Channels;

namespace SimilarityCalculator.services
{
    public class SimilarityCalculator : IHostedService
    {
        private readonly RedisService _redisService;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public SimilarityCalculator(RedisService redisService, IConnectionFactory connectionFactory)
        {
            _redisService = redisService;
            _connectionFactory = connectionFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "similarity_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);
            _channel.ExchangeDeclare("events", ExchangeType.Topic);


            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var jsonMessage = Encoding.UTF8.GetString(body);
                try
                {
                    var message = JsonConvert.DeserializeObject<TextProcessingMessage>(jsonMessage);

                    var id = message.Id;
                    var userText = message.UserText;

                    Console.WriteLine($"[CONSOLE] Получено сообщение: Id = {id}, UserText = {userText}");

                    await ProcessTextAsync(id, userText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONSOLE] Ошибка при обработке сообщения: {ex.Message}");
                }
            };
            _channel.BasicConsume(queue: "similarity_tasks", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Close();
            _connection.Close();
            return Task.CompletedTask;
        }

        private async Task ProcessTextAsync(string id, string? UserText)
        {
            try
            {
                string textHash = GetTextHash(UserText);
                double similarity = await _redisService.IsDuplicateTextAsync(textHash) ? 1.0 : 0.0;
                string similarityKey = "SIMILARITY-" + id;

                await _redisService.SaveSimilarityAsync(similarityKey, similarity);

                Console.WriteLine($"[CONSOLE] Завершено вычисление для текста с ID: {id} | Similarity: {similarity}");

                var eventMessage = new
                {
                    EventType = "SimilarityCalculated",
                    Id = "TEXT" + id,
                    Similarity = similarity
                };
                string json = JsonConvert.SerializeObject(eventMessage);
                var body = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(exchange: "events", routingKey: "similarity_events", basicProperties: null, body: body);
            }
            catch (Exception ex)
            {
                var eventMessage = new
                {
                    EventType = "SimilarityCalculatedError",
                    Id = "TEXT" + id,
                    Similarity = "null"
                };
                string json = JsonConvert.SerializeObject(eventMessage);
                var body = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(exchange: "events", routingKey: "similarity_events", basicProperties: null, body: body);

                Console.WriteLine($"[CONSOLE] Ошибка вычисления rank и similarity: {ex.Message}");
            }
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