using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Valuator.Services;
using Valuator.Pages;

using Newtonsoft.Json;

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
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "rank_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);


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
            _channel.BasicConsume(queue: "rank_tasks", autoAck: true, consumer: consumer);

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
                double rank = CalculateRank(UserText);
                string rankKey = "RANK-" + id;

                await _redisService.SaveRankAsync(rankKey, rank);

                Console.WriteLine($"[CONSOLE] Завершено вычисление для текста с ID: {id} | Rank: {rank}");
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
    }
}