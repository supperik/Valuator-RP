using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Valuator.Services;
using Valuator.Pages;

using Newtonsoft.Json;
using Valuator.Sharding;

namespace RankCalculator.services
{
    public class RankCalculator : IHostedService
    {
        private readonly RedisShardManager _redisShardManager;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RankCalculator(RedisShardManager redisShardManager, IConnectionFactory connectionFactory)
        {
            _redisShardManager = redisShardManager;
            _connectionFactory = connectionFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "rank_tasks", durable: false, exclusive: false, autoDelete: false, arguments: null);
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
                    var region = _redisShardManager.GetRegion(id);
                    _redisShardManager.LogLookup(id, region.ToString());
                    var regionDb = _redisShardManager.GetRedisServiceByRegion(region.ToString());

                    string userText = await regionDb.GetTextAsync("UNHASHED-TEXT-" + id);

                    Console.WriteLine($"[CONSOLE] Получено сообщение: Id = {id}, UserText = {userText}");

                    await ProcessTextAsync(id, userText, regionDb);
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

        private async Task ProcessTextAsync(string id, string? UserText, RedisService redis)
        {
            try
            {
                double rank = CalculateRank(UserText);
                string rankKey = "RANK-" + id;


                Thread.Sleep(1000);
                await redis.SaveRankAsync(rankKey, rank);

                Console.WriteLine($"[CONSOLE] Завершено вычисление для текста с ID: {id} | Rank: {rank}");

                var eventMessage = new
                {
                    EventType = "RankCalculated",
                    Id = "TEXT" + id,
                    Rank = rank
                };
                string json = JsonConvert.SerializeObject(eventMessage);
                var body = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(exchange: "events", routingKey: "rank_events", basicProperties: null, body: body);
            }
            catch (Exception ex)
            {
                var eventMessage = new
                {
                    EventType = "RankCalculatedError",
                    Id = "TEXT" + id,
                    Rank = "null"
                };
                string json = JsonConvert.SerializeObject(eventMessage);
                var body = Encoding.UTF8.GetBytes(json);
                _channel.BasicPublish(exchange: "events", routingKey: "rank_events", basicProperties: null, body: body);

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