using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StackExchange.Redis;
using Valuator.Services;

namespace RankCalculator.services;
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IConnectionFactory>(sp =>
                {
                    return new ConnectionFactory()
                    {
                        HostName = "localhost",
                        Port = 5672
                    };
                });

                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect("localhost:6379")
                );

                services.AddSingleton<RedisService>();

                services.AddHostedService<RankCalculator>();

                services.AddLogging(builder => builder.AddConsole());
            });
}