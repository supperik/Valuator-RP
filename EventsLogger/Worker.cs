using EventsLogger.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EventsLogger;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "rank_events", durable: false, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(queue: "similarity_events", durable: false, exclusive: false, autoDelete: false, arguments: null);

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
            var simEvent = JsonConvert.DeserializeObject<SimilarityCalculatedEvent>(message);

            Console.WriteLine($"[SimilarityCalculated] ID: {simEvent.Id}, Similarity: {simEvent.Similarity}");
        };

        channel.BasicConsume(queue: "rank_events", autoAck: true, consumer: rankConsumer);
        channel.BasicConsume(queue: "similarity_events", autoAck: true, consumer: similarityConsumer);

        Console.WriteLine("[EventLogger] �������� �� ������� ��������...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

}
