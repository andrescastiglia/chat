using Api.Services;

// Services
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddSingleton<ICommandService, CommandService>();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<ISecurityService, SecurityService>();
builder.Services.AddSingleton<IStockService, StockService>();

// Application
var app = builder.Build();
app.MapControllers();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};
app.UseWebSockets(webSocketOptions);

app.Run();
