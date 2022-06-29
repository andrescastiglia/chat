using Api.Services;
using RabbitMQ.Client;

// Services
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddSingleton<ICommandService, CommandService>();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<ISecurityService, SecurityService>();
builder.Services.AddSingleton<IStockService, StockService>();
builder.Services.AddSingleton<IStooqService, StooqService>();

builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>(serviceProvider =>
{
    return new ConnectionFactory()
    {
        HostName = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? throw new Exception("RABBIT_HOST is missing"),
        Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBIT_PORT") ?? throw new Exception("RABBIT_PORT is missing")),
        UserName = Environment.GetEnvironmentVariable("RABBIT_USERNAME") ?? throw new Exception("RABBIT_USERNAME is missing"),
        Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD") ?? throw new Exception("RABBIT_PASSWORD is missing"),
        VirtualHost = Environment.GetEnvironmentVariable("RABBIT_VHOST") ?? throw new Exception("RABBIT_VHOST is missing")
    };
});

builder.Services.AddSingleton<IModel>(serviceProvider =>
{
    var factory = serviceProvider.GetService<IConnectionFactory>();
    var connection = factory?.CreateConnection();
    var model = connection?.CreateModel();
    return model ?? throw new Exception("Failed to create HttpClient");
});

builder.Services.AddSingleton<HttpClient>();

// Application
var app = builder.Build();
app.MapControllers();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};
app.UseWebSockets(webSocketOptions);

app.Run();
