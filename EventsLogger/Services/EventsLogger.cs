using EventsLoggerNamespace.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EventsLoggerNamespace.Services;

public class EventsLogger : BackgroundService
{
    private readonly ILogger<EventsLogger> _logger;

    public EventsLogger(ILogger<EventsLogger> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare("events", ExchangeType.Topic);

        var similarityQueue = channel.QueueDeclare(queue: "", durable: false, exclusive: true, autoDelete: true);
        var rankQueue = channel.QueueDeclare(queue: "", durable: false, exclusive: true, autoDelete: true);

        channel.QueueBind(queue: similarityQueue.QueueName, exchange: "events", routingKey: "similarity_events");
        channel.QueueBind(queue: rankQueue.QueueName, exchange: "events", routingKey: "rank_events");

        var rankConsumer = new EventingBasicConsumer(channel);
        rankConsumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var rankEvent = JsonConvert.DeserializeObject<RankCalculatedEvent>(message);

            Console.WriteLine($"[RankCalculated] ID: {rankEvent.Id}, Rank: {rankEvent.Rank}");
        };

        var similarityConsumer = new EventingBasicConsumer(channel);
        similarityConsumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var similarityEvent = JsonConvert.DeserializeObject<SimilarityCalculatedEvent>(message);

            Console.WriteLine($"[SimilarityCalculated] ID: {similarityEvent.Id}, Similarity: {similarityEvent.Similarity}");
        };

        channel.BasicConsume(queue: rankQueue.QueueName, autoAck: true, consumer: rankConsumer);
        channel.BasicConsume(queue: similarityQueue.QueueName, autoAck: true, consumer: similarityConsumer);

        Console.WriteLine("[EventLogger] Подписка на события запущена...");


        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
