using System.Text;
using Api.Model;
using Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public interface IStockService
{
    void Enqueue(string code);
}

public class StockService : IStockService, IDisposable
{
    private readonly ILogger<StockService> _logger;
    private IMessageService _messageService;
    private ConnectionFactory _factory;
    private IConnection _connection;
    private IModel _queue;

    public StockService(ILogger<StockService> logger, IMessageService messageService)
    {
        _logger = logger;
        _messageService = messageService;

        _factory = new ConnectionFactory()
        {
            HostName = Environment.GetEnvironmentVariable("RABBIT_HOST"),
            Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBIT_PORT")),
            UserName = Environment.GetEnvironmentVariable("RABBIT_USERNAME"),
            Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD"),
            VirtualHost = Environment.GetEnvironmentVariable("RABBIT_VHOST"),
        };

        _connection = _factory.CreateConnection();
        _queue = _connection.CreateModel();

        var consumer = new EventingBasicConsumer(_queue);
        consumer.Received += Received;

        _queue.BasicConsume(Environment.GetEnvironmentVariable("RABBIT_QUEUE"), true, consumer);
    }

    public void Dispose()
    {
        _queue.Dispose();
        _connection.Dispose();
    }

    public void Enqueue(string code)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.BasicPublish(Environment.GetEnvironmentVariable("RABBIT_EXCHANGE"), string.Empty, null, Encoding.UTF8.GetBytes(code));
    }

    private async void Received(object? model, BasicDeliverEventArgs args)
    {
        var code = Encoding.UTF8.GetString(args.Body.ToArray());

        var uri = Environment.GetEnvironmentVariable("STOCK_URI")?.Replace("#code#", code);

        using var httpClient = new HttpClient();

        var raw = await httpClient.GetByteArrayAsync(uri);
        if (raw is null)
        {
            _logger.LogWarning($"Failed to download stock {uri}");
        }
        else
        {
            var message = Encoding.UTF8.GetString(raw);
            var lines = message?.Split('\n');
            if (lines?.Length > 0)
            {
                var fields = lines[1].Split(',');
                if (fields?.Length == 8)
                {
                    var response = new MessageOut($"{fields[0]} quote is ${fields[3]} per share");
                    _messageService.Broadcast(response);
                    return;
                }
            }
            _logger.LogWarning($"Failed to parse {message}");
        }
    }
}