using System.Text;
using Api.Model;
using Api.Services;

public interface IStockService
{
    void Enqueue(string code);
}

public class StockService : IStockService
{
    private const string STOCK_URI = "https://stooq.com/q/l/?s=#code#&f=sd2t2ohlcv&h&e=csv";

    private readonly ILogger<StockService> _logger;
    private IMessageService _messageService;


    public StockService(ILogger<StockService> logger, IMessageService messageService)
    {
        _logger = logger;
        _messageService = messageService;
    }

    public async void Enqueue(string code)
    {
        /// TODO Enqueue in RabbitMQ
        var uri = STOCK_URI.Replace("#code#", code);

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