using System.Text;
using Api.Model;
using Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Api.Services;

public interface IStooqService
{
}

public class StooqService : IStooqService, IDisposable
{
    private readonly ILogger<StooqService> _logger;
    private IMessageService _messageService;
    private IModel _queue;
    private HttpClient _httpClient;
    private string _stooqUri;

    public StooqService(ILogger<StooqService> logger, IMessageService messageService, IModel queue, HttpClient httpClient)
    {
        _logger = logger;
        _messageService = messageService;
        _queue = queue;
        _httpClient = httpClient;
        _stooqUri = Environment.GetEnvironmentVariable("STOCK_URI") ?? "";

        var consumer = new EventingBasicConsumer(_queue);
        consumer.Received += Received;
        _queue.BasicConsume(Environment.GetEnvironmentVariable("RABBIT_QUEUE"), true, consumer);
    }

    public void Dispose()
    {
        _queue.Dispose();
        _httpClient.Dispose();
    }

    private async void Received(object? model, BasicDeliverEventArgs args)
    {
        var code = Encoding.UTF8.GetString(args.Body.ToArray());

        var uri = _stooqUri.Replace("#code#", code);

        var raw = await _httpClient.GetByteArrayAsync(uri);
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