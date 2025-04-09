using EventsLoggerNamespace.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<EventsLogger>();

var host = builder.Build();
host.Run();
