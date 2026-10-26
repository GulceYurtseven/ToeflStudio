var builder = Host.CreateApplicationBuilder(args);
// Durable jobs will be registered when Assessment is implemented.

var host = builder.Build();
host.Run();
